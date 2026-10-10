import moduleHelper from './module-helper';
import { MD5 } from './md5';


let liveBattleInstance = null;
let gameClubInstance = null;

function ensureGameClub() {
    if (gameClubInstance)
        return gameClubInstance;
    if (!GameGlobal.miniGameCommon || typeof GameGlobal.miniGameCommon.createGameClub !== 'function')
        return null;
    gameClubInstance = GameGlobal.miniGameCommon.createGameClub();
    return gameClubInstance;
}

try {
    if (typeof requirePlugin !== 'undefined') {
      const createMiniGameCommon = requirePlugin('MiniGameCommon', {
        enableRequireHostModule: true,
        customEnv: {
          wx,
        },
      }).default;
      const miniGameCommon = createMiniGameCommon();
      if (typeof miniGameCommon === 'undefined' || typeof miniGameCommon.canIUse === 'undefined') {
        // 插件初始化失败
        console.error('miniGameCommon create error');
      } else {
        // 插件初始化成功
        GameGlobal.miniGameCommon = miniGameCommon;
        console.log('miniGameCommon create success');
      }
    }
  } catch (e) {
    // 基础库版本过低
    console.error(e);
  }


export default {
    WXMiniGameCommonGetUserLabelV2:function(gift_scene,gift_name,gift_value,gift_value_success,tt_action,callBackId)
    {
        //获取用户标签
        if (GameGlobal.miniGameCommon == undefined) {
            console.error('miniGameCommon is undefined');
            return;
        }
        let reportData = '{"gift_scene":"'+gift_scene+'","gift_name":"'+gift_name+'","gift_value":'+gift_value+',"gift_value_success":'+gift_value_success+'}';
        console.log('miniGameCommon getUserLabel v2:',reportData);
        GameGlobal.miniGameCommon.getUserLabel(
            {
                reportData:reportData,
                labelId:'iaa_feature_v2'
            }
        ).then((res)=>{
            console.log('miniGameCommon getuserLabel v2 success:',res);
            moduleHelper.send(
                'OnWXMiniGameCommonGetUserLabelV2',
                JSON.stringify({
                    callBackId:callBackId,
                    code: 0,
                    errMsg: res.errMsg,
                    encryptedData:res.encryptedData,
                    iv:res.iv,
                    cloudID:res.cloudID,
                    ipu:gift_value_success,
                    tt_action:tt_action,
                    //////-------/////
                    gift_scene:gift_scene,
                    gift_name:gift_name,
                    gift_value:gift_value,
                    gift_value_success:gift_value_success
                })
            )
        }).catch((res)=>{
            console.log('miniGameCommon getUserLabel v2 fail:',res);
            moduleHelper.send(
                'OnWXMiniGameCommonGetUserLabelV2',
                JSON.stringify({
                    callBackId:callBackId,
                    code: -1,
                    errMsg: res.errMsg,
                })
            )
        });
    },
    WXMiniGameCommonGetUserLabel:function(gift_scene,gift_name,gift_value,gift_value_success,tt_action,callBackId)
    {
        //获取用户标签
        if (GameGlobal.miniGameCommon == undefined) {
            console.error('miniGameCommon is undefined');
            return;
        }
        let reportData = '{"gift_scene":"'+gift_scene+'","gift_name":"'+gift_name+'","gift_value":'+gift_value+',"gift_value_success":'+gift_value_success+'}';
        console.log('miniGameCommon getUserLabel v1:',reportData);
        GameGlobal.miniGameCommon.getUserLabel(
            {
                reportData:reportData,
                labelId:'iaa_feature'
            }
        ).then((res)=>{
            console.log('miniGameCommon getuserLabel v1 success:',res);
            moduleHelper.send(
                'OnWXMiniGameCommonGetUserLabel',
                JSON.stringify({
                    callBackId:callBackId,
                    code: 0,
                    errMsg: res.errMsg,
                    encryptedData:res.encryptedData,
                    iv:res.iv,
                    cloudID:res.cloudID,
					ipu:gift_value_success,
					tt_action:tt_action
                })
            )
        }).catch((res)=>{
            console.log('miniGameCommon getUserLabel v1 fail:',res);
            moduleHelper.send(
                'OnWXMiniGameCommonGetUserLabel',
                JSON.stringify({
                    callBackId:callBackId,
                    code: -1,
                    errMsg: res.errMsg,
                })
            )
        });
    },
	WXGetSign:function(appid,appKey)
	{
		return MD5.hex_md5(appid + appKey);
	},
    WXMiniGameCommonGetLiveBattleRankManager:function(callBackId)
    {
        if (!GameGlobal.miniGameCommon) {
            console.error('[Game] getRankManager not available !GameGlobal.miniGameCommon');
            moduleHelper.send(
                'OnWXMiniGameCommonGetLiveBattleRankManager',
                JSON.stringify({
                    callBackId: callBackId,
                    code: -1,
                    errMsg: 'getRankManager not available !GameGlobal.miniGameCommon'
                })
            );
            return;
        }
        if (typeof GameGlobal.miniGameCommon.getRankManager !== 'function') {
            console.error('[Game] getRankManager not available typeof GameGlobal.miniGameCommon.getRankManager !== function');
            moduleHelper.send(
                'OnWXMiniGameCommonGetLiveBattleRankManager',
                JSON.stringify({
                    callBackId: callBackId,
                    code: -1,
                    errMsg: 'getRankManager not available typeof GameGlobal.miniGameCommon.getRankManager !== function'
                })
            );
            return;
        }

        liveBattleInstance = GameGlobal.miniGameCommon.getRankManager({ type: 'liveBattle' });
        if (!liveBattleInstance) {
            console.error('[Game] liveBattle RankManager not available !liveBattleInstance');
            moduleHelper.send(
                'OnWXMiniGameCommonGetLiveBattleRankManager',
                JSON.stringify({
                    callBackId: callBackId,
                    code: -1,
                    errMsg: 'liveBattle RankManager not available !liveBattleInstance'
                })
            );
            return;
        }

        liveBattleInstance.onChallengeStart((payload) => {
            console.log('[GameLiveBattle] onChallengeStart:', payload);
            moduleHelper.send(
                'OnWXMiniGameCommonLiveBattleChallengeStart',
                JSON.stringify({
                    stageName: payload.stageName,
                    scoreKey: payload.scoreKey,
                    subScoreKey: payload.subScoreKey
                })
            );
        });

        moduleHelper.send(
            'OnWXMiniGameCommonGetLiveBattleRankManager',
            JSON.stringify({
                callBackId: callBackId,
                code: 0,
                errMsg: 'success'
            })
        );
    },
    WXMiniGameCommonLiveBattleUpdateScore:function(score, callBackId)
    {
        if (!liveBattleInstance) {
            console.error('[Game] liveBattle RankManager not initialized');
            moduleHelper.send(
                'OnWXMiniGameCommonLiveBattleUpdateScore',
                JSON.stringify({
                    callBackId: callBackId,
                    code: -1,
                    errMsg: 'liveBattle RankManager not initialized'
                })
            );
            return;
        }

        liveBattleInstance.update({
            score: score,
            success: (res) => {
                console.log('[RankManager] success', res);
                moduleHelper.send(
                    'OnWXMiniGameCommonLiveBattleUpdateScore',
                    JSON.stringify({
                        callBackId: callBackId,
                        code: 0,
                        errMsg: 'success',
                        res: res
                    })
                );
            },
            fail: (err) => {
                console.error('[RankManager] fail', err);
                moduleHelper.send(
                    'OnWXMiniGameCommonLiveBattleUpdateScore',
                    JSON.stringify({
                        callBackId: callBackId,
                        code: -1,
                        errMsg: err
                    })
                );
            },
            complete: (res) => {
                console.log('[RankManager] complete', res);
            }
        });
    },
    WXMiniGameCommonCanIUseShareImageToGameCenter: function () {
        return !!(GameGlobal.miniGameCommon
            && typeof GameGlobal.miniGameCommon.canIUse === 'function'
            && GameGlobal.miniGameCommon.canIUse('shareImageToGameCenter'));
    },
    WXMiniGameCommonShareImageToGameCenter: function (path, title, content, callBackId) {
        function send(code, errMsg) {
            moduleHelper.send(
                'OnWXMiniGameCommonShareImageToGameCenter',
                JSON.stringify({
                    callBackId: callBackId,
                    code: code,
                    errMsg: errMsg || ''
                })
            );
        }
        if (!GameGlobal.miniGameCommon) {
            console.error('[GameClub] shareImageToGameCenter not available !GameGlobal.miniGameCommon');
            send(-1, 'miniGameCommon is undefined');
            return;
        }
        if (typeof GameGlobal.miniGameCommon.canIUse !== 'function'
            || !GameGlobal.miniGameCommon.canIUse('shareImageToGameCenter')) {
            console.error('[GameClub] canIUse shareImageToGameCenter false');
            send(-1, 'canIUse shareImageToGameCenter false');
            return;
        }
        var club = ensureGameClub();
        if (!club || typeof club.shareImageToGameCenter !== 'function') {
            console.error('[GameClub] createGameClub / shareImageToGameCenter not available');
            send(-1, 'createGameClub not available');
            return;
        }
        var done = false;
        function finish(code, errMsg) {
            if (done) return;
            done = true;
            send(code, errMsg);
        }
        try {
            var ret = club.shareImageToGameCenter({
                path: path,
                title: title,
                content: content,
                success: function (res) {
                    console.log('[GameClub] shareImageToGameCenter success', res);
                    finish(0, (res && res.errMsg) || 'success');
                },
                fail: function (err) {
                    console.error('[GameClub] shareImageToGameCenter fail', err);
                    finish(-1, (err && err.errMsg) || String(err));
                }
            });
            if (ret && typeof ret.then === 'function') {
                ret.then(function (res) {
                    console.log('[GameClub] shareImageToGameCenter then', res);
                    finish(0, (res && res.errMsg) || 'success');
                }).catch(function (err) {
                    console.error('[GameClub] shareImageToGameCenter catch', err);
                    finish(-1, (err && err.errMsg) || String(err));
                });
            }
        } catch (e) {
            console.error('[GameClub] shareImageToGameCenter exception', e);
            finish(-1, e && e.message ? e.message : String(e));
        }
    }
}