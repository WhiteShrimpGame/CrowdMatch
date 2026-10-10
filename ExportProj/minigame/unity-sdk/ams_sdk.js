import { SDK } from "./wxams";

// 直玩广告状态由 C# 现有 FeedPlay 流程统一检测并通知 AMS 接入层。
// 官方 wxams.js 保持原样，避免后续升级 SDK 时覆盖项目侧逻辑。
var directGameAmsGate = {
  blocked: false,
  sdk: null,
  originalTrack: null,
  pendingTracks: [],

  init: function (sdk) {
    this.sdk = sdk;
    this.originalTrack = sdk.track;
  },

  setMasked: function (isMasked) {
    if (isMasked) {
      this.block();
    }
    else {
      this.release();
    }
  },

  block: function () {
    if (this.blocked || !this.sdk || !this.originalTrack) {
      return;
    }

    this.blocked = true;
    var gate = this;
    this.sdk.track = function () {
      if (!gate.blocked) {
        return gate.originalTrack.apply(gate.sdk, arguments);
      }

      var args = Array.prototype.slice.call(arguments);
      // 保留 SDK 特殊方法写入的 Symbol 参数，同时避免调用方修改顶层参数。
      if (args[1] && typeof args[1] === "object") {
        args[1] = Object.assign({}, args[1]);
      }
      gate.pendingTracks.push(args);

      console.log("WXAMS report cached while direct game mask exists:", args[0]);
      return {
        code: 0,
        message: "直玩广告蒙层期间已缓存"
      };
    };

    console.log("WXAMS direct game mask exists, pause registration and business reports");
  },

  release: function () {
    if (!this.sdk || !this.originalTrack) {
      return;
    }

    if (!this.blocked) {
      // C# 可能先收到戳破事件；同时触发一次 flush，发送官方 SDK 内已缓存的数据。
      this.sdk.flush();
      return;
    }

    this.blocked = false;
    this.sdk.track = this.originalTrack;

    var pendingTracks = this.pendingTracks;
    this.pendingTracks = [];
    console.log("WXAMS direct game mask removed, flush cached reports:", pendingTracks.length);

    for (var i = 0; i < pendingTracks.length; i++) {
      try {
        this.originalTrack.apply(this.sdk, pendingTracks[i]);
      }
      catch (error) {
        console.warn("WXAMS replay cached report failed:", pendingTracks[i][0], error);
      }
    }

    // 确保注册和业务数据在戳破蒙层后立即触发发送。
    this.sdk.flush();
  }
};

export default {
  InitAMS: function (ams_actionid, ams_secretkey, ams_appid) {
    if (GameGlobal.dnSDK != undefined) {
      console.log("WXAMS InitAMS already initialized, skip");
      return;
    }
    try {
      GameGlobal.dnSDK = new SDK({
        user_action_set_id: ams_actionid,
        secret_key: ams_secretkey,
        appid: ams_appid
      });
      directGameAmsGate.init(GameGlobal.dnSDK);
    }
    catch (error) {
      console.warn("WXAMS Init failed, error:", error);
      return;
    }
    console.log("WXAMS Init success");
    window.WXWASMSDK.IsInitWXAMS = true;
  },
  SetAMSDirectGameMaskStatus: function (isMasked) {
    directGameAmsGate.setMasked(!!isMasked);
  },
  START_LOAD: function () {
    var result = GameGlobal.dnSDK.track('START_LOAD',{});
    if (result != undefined && result.code !== 0) {
      console.warn("WXAMS START_LOAD failed, code:", result.code, "message:", result.message);
    }
    else if (result != undefined && result.code === 0) {
      console.log("WXAMS START_LOAD success");
    }
  }
}
