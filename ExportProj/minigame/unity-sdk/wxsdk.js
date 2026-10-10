import moduleHelper from './module-helper';

let feedbackButton = null;
let GameConfig = null;
export default {
    GetJsonValue: function (key) {
        if (GameConfig == null) {
            let res = wx.getFileSystemManager().readFileSync("GameConfig.json", 'utf8', 0);
            GameConfig = JSON.parse(res);
        }

        if (GameConfig == null) {
            return "";
        }
        if (GameConfig[key] == undefined) {
            return ""
        }

        return GameConfig[key];
    },
    GetABValue: function (key) {
        const res = wx.getExptInfoSync([key])
        if (res[key] === undefined) {
            console.log("GetABValue undefined 0");
            return "0";
        }
        else {
            console.log("GetABValue: " + res[key]);
            return res[key];
        }
    },
    WXGetGameExptInfo: function () {
        if (wx.getGameExptInfo === undefined) {
            console.log("WXGetGameExptInfo wx.getGameExptInfo is not supported in this environment.");
            return;
        }
        wx.getGameExptInfo({
            keyList: [],
            success(res) {
                if(res.list == undefined || res.list.length == 0)
                {
                    console.log("WXGetGameExptInfo wx.getGameExptInfo success, list is empty");
                    return;
                }
                var str_list = JSON.stringify(res.list);
                console.log("WXGetGameExptInfo wx.getGameExptInfo success list:",str_list)
                moduleHelper.send(
                    'OnWXGetGameExptInfoCallback',
                    str_list
                );
            }
          });
    },
    CreateFeedBackButton: function (width, height, positionX, positionY, isShow) {
        console.log("call CreateFeedBackButton ");
        if (feedbackButton == null) {
            feedbackButton = wx.createFeedbackButton({
                type: 'text',
                text: isShow ? 'FeedBack' : '',
                style: {
                    left: positionX,
                    top: positionY,
                    width: width,
                    height: height,
                    lineHeight: 40,
                    backgroundColor: isShow ? '#000000' : '',
                    color: '#ffffff',
                    textAlign: 'center',
                    fontSize: 16,
                    borderRadius: 4
                }
            })
        }
        else
        {
            //刷新位置
            feedbackButton.style.left = positionX;
            feedbackButton.style.top = positionY;
            feedbackButton.style.width = width;
            feedbackButton.style.height = height;
            feedbackButton.style.backgroundColor = isShow ? '#000000' : '';
        }
        feedbackButton.show();
    },
    HideFeedBackButton: function () {
        if (feedbackButton != null) {
            feedbackButton.hide();
        }
    },
    CheckIsAddedToMyMiniProgram(invokeId) {
        console.log("CheckIsAddedToMyMiniProgram");
        if (wx.checkIsAddedToMyMiniProgram) {
            wx.checkIsAddedToMyMiniProgram && wx.checkIsAddedToMyMiniProgram({
                success(res) {
                    console.log("CheckIsAddedToMyMiniProgram res.added:" + res.added);
                    moduleHelper.send(
                        'OnCheckIsAddedToMyMiniProgram',
                        JSON.stringify({
                            id: invokeId,
                            code: 0,
                            added: res.added,
                        })
                    )
                },
                fail(res) {
                    console.log("CheckIsAddedToMyMiniProgram Failure");
                    moduleHelper.send(
                        'OnCheckIsAddedToMyMiniProgram',
                        JSON.stringify({
                            id: invokeId,
                            code: 0,
                            added: false,
                        })
                    )
                }
            });
        } else {
            moduleHelper.send(
                'OnCheckIsAddedToMyMiniProgram',
                JSON.stringify({
                    id: invokeId,
                    code: -1,
                    added: false,
                })
            )
        }
    },
    RequireOpenPrivacyAuthorize() {
        wx.requirePrivacyAuthorize && wx.requirePrivacyAuthorize({
            success: res => {
                // 非标准API的方式处理用户个人信息
                moduleHelper.send(
                    'OnRequirePrivacyAuthorizeCallback',
                    JSON.stringify({
                        success: 1,
                        errMsg: res.errMsg,
                    })
                );
            },
            fail: (res) => {
                moduleHelper.send(
                    'OnRequirePrivacyAuthorizeCallback',
                    JSON.stringify({
                        success: 0,
                        errMsg: res.errMsg,
                    })
                );
             },
            complete: () => { }
        });
    },
    GetPrivacySetting(){
        wx.getPrivacySetting({
            success: res => {
                var code=0;
                if(res.needAuthorization)
                    code=1;
                moduleHelper.send(
                    'OnGetPrivacySettingCallback',
                    JSON.stringify({
                        code: code,
                        privacyContractName: res.privacyContractName,
                    })
                );
              // 返回结果为: res = { needAuthorization: true/false, privacyContractName: '《xxx隐私保护指引》' }
            },
            fail: () => {
                moduleHelper.send(
                    'OnGetPrivacySettingCallback',
                    JSON.stringify({
                        code: -1,
                    })
                );
            },
            complete: () => {}
          })
    },
    OnNeedPrivacyAuthorization(event) {

        var str = 'exposureAuthorization';
        if (event == 2) {
            str = 'agree';
        } else if (event == 3) {
            str = 'disagree';
        }
        console.log("微信隐私 上报隐私行为：" + str);
        wx.onNeedPrivacyAuthorization && wx.onNeedPrivacyAuthorization(resolve => {
            resolve({ event: str })
        })
    },
    OpenPrivacyContract() {
        wx.openPrivacyContract && wx.openPrivacyContract({
            success: () => { }, // 打开成功
            fail: () => { }, // 打开失败
            complete: () => { }
        });
    },
    SubGameUpdate() {
        wx.requestSubscribeSystemMessage({
            msgTypeList: ['SYS_MSG_TYPE_WHATS_NEW'],
            success(res) {
                console.log(res)
                var ret = res['SYS_MSG_TYPE_WHATS_NEW'] == 'accept' ? "1" : "0";
                moduleHelper.send(
                    'RequestSubscribeSystemMessageCallBack',
                    ret
                );
            },
            fail(err) {
                console.error(err)
                moduleHelper.send(
                    'RequestSubscribeSystemMessageCallBack',
                    "0"
                );
            }
        });
    },
    GetSubGameUpdateStatus() {
        /*wx.getWhatsNewSubscriptionsSetting({
            msgType: 1,    // 消息类型，1=游戏更新提醒，目前只有这种类型
            success(res) {
                console.log(res.status)
                // res.status === 1 
            },
            fail(err) {
                console.error(err)
            }
        })
        */
        wx.getSetting({
            withSubscriptions: true,
            success(res) {
                var ret = res.subscriptionsSetting['SYS_MSG_TYPE_WHATS_NEW'] == 'accept' ? "1" : "0";
                moduleHelper.send(
                    'GetSubGameUpdateStatusCallBack',
                    ret
                );

            },
            fail(res) {
                moduleHelper.send(
                    'GetSubGameUpdateStatusCallBack',
                    "0"
                );

            },
        });
    },
    RequestSubscribe(tmplId)
    {
        wx.requestSubscribeMessage({
            tmplIds: [tmplId],
            success (res) {
              console.log(res)
              moduleHelper.send(
                'RequestSubscribeCallBack',
                JSON.stringify(res)
            );
            },
            fail(res) {
                console.log(res)
                moduleHelper.send(
                    'RequestSubscribeCallBack',
                    JSON.stringify(res)
                );
            }
          })
    },
    RequestSubscribeMulti(tmplIds)
    {
        wx.requestSubscribeMessage({
            tmplIds: tmplIds,
            success (res) {
              console.log(res)
              moduleHelper.send(
                'RequestSubscribeCallBack',
                JSON.stringify(res)
            );
            },
            fail(res) {
                console.log(res)
                moduleHelper.send(
                    'RequestSubscribeCallBack',
                    JSON.stringify(res)
                );
            }
          })
    },
	GetAuthorizeSetting(_withSubscriptions)
	{
        _withSubscriptions = _withSubscriptions == 1;
		wx.getSetting({
            withSubscriptions: _withSubscriptions,
            success(res) {
                console.log(res)
                if (res.authSetting != undefined) {
					if(res.subscriptionsSetting != undefined)
					{
						res.authSetting.subscriptionsSetting = res.subscriptionsSetting;
					}
					
                    moduleHelper.send(
                        'GetAuthorizeSettingCallBack',
                        JSON.stringify(res.authSetting)
                    );
                }else{
                    moduleHelper.send(
                        'GetAuthorizeSettingCallBack',
                        ""
                    );
                }
                
            },
            fail(res) {
                console.log("GetAuthorizeSetting fail");
                moduleHelper.send(
                    'GetAuthorizeSettingCallBack',
                    ""
                );
            },
        });
	},
	GetWXDATA_CDN()
	{
		return GameGlobal.managerConfig.DATA_CDN;
	},
    GetOrCreatePageManager(tag,createCallback)
    {
        const stage_init="init";//刚创建
        const stage_preload="preloading";//预加载中
        const stage_ready="ready";//预加载完成
        const stage_show="show";//显示
        const stage_destroy="destroy";//关闭后等待销毁

        if (!window.wx.createPageManager) {
            console.log('当前基础库版本暂不支持。');
            return null;
        }
        //存不同tag的对象的字典
        window.PageManagerHashs = window.PageManagerHashs || {};
        if(window.PageManagerHashs[tag]!=null && window.PageManagerHashs[tag].stage!=stage_destroy)
        {
            //有现成的，现成的触发创建回调
            return window.PageManagerHashs[tag];
        } else {
            //新建一个
            var tmpPageManager = window.wx.createPageManager();
            window.PageManagerHashs[tag] = tmpPageManager;
            window.PageManagerHashs[tag].stage=stage_init;//状态
            window.PageManagerHashs[tag].waitReady=false;//是否在等待ready后显示

            tmpPageManager.on(
                'show', // show | destroy | error
                (res) => {
                    //注册预加载事件
                    if(window.PageManagerHashs!=null&&window.PageManagerHashs[tag]!=null){
                        window.PageManagerHashs[tag].waitReady=false;
                        window.PageManagerHashs[tag].stage=stage_show;
                    }
                    console.log('page manager show,tag:'+tag+",res:" +JSON.stringify(res));
                    // console.log("window.PageManagerHashs[tag]:"+window.PageManagerHashs[tag]);
                }
            );
            tmpPageManager.on(
                'error', // show | destroy | error
                (res) => {
                    //主动监听报错接口，避免出现报错卡流程
                    if(window.PageManagerHashs!=null&&window.PageManagerHashs[tag]!=null){
                        window.PageManagerHashs[tag].waitReady=false;
                    }
                    console.error('page manager error,tag:'+tag+",res:" +JSON.stringify(res));
                    // console.error("window.PageManagerHashs[tag]:"+window.PageManagerHashs[tag]);
                }
            );
            tmpPageManager.on(
                'destroy', // show | destroy | error
                (res) => {
                    //注册关闭清空引用流程
                    console.log('page manager destroy,remove tag:'+tag+",res:" +JSON.stringify(res));
                    if(window.PageManagerHashs!=null&&window.PageManagerHashs[tag]!=null)
                    {
                        window.PageManagerHashs[tag].waitReady=false;
                        window.PageManagerHashs[tag] = null;
                    }
                }
              );

            //触发创建事件
            if(createCallback != null && typeof createCallback === 'function' && createCallback.length == 1){
                createCallback(tmpPageManager);
            }

            return tmpPageManager;
        }
    },
    DestroyPageManager(tag)
    {
        if(window.PageManagerHashs!=null&&window.PageManagerHashs[tag]!=null)
        {
            // 推荐的界面会关闭后不需要额外调用 destroy ，其他界面不确定
            window.PageManagerHashs[tag].destroy();
            // window.PageManagerHashs[tag] = null;
        }
    },
    GetOrCreateRecommendPageManager()
    {
        const tag="recommend";

        var tmpPageManager = window.WXWASMSDK.GetOrCreatePageManager(tag ,(pageManager)=>{
            //创建回调中注册事件
            pageManager.on(
                'destroy', // show | destroy | error
                (res) => {
                    var isRecommended= "0";
                    if (res!=null && res.isRecommended!=null){
                        console.log('recommend component close：', res.isRecommended);
                        isRecommended=res.isRecommended?"1":"0";
                    } else {
                        isRecommended= "-1";
                        console.log('recommend component close：res is null');
                    }

                    // window.recommendPageManager.destroy();//最新的微小运行时，不需要手动调用 destroy 了
                    // window.WXWASMSDK.DestroyPageManager(tag);//在创建时自动注册清空的 destroy 事件

                    moduleHelper.send(
                        'OnDestroyRecommendCallback',
                        isRecommended,
                    );
                },
            );
        });

        return tmpPageManager;
    },
    async PreLoadPageManager(pageManager,argsObj)
    {
        const stage_init="init";//刚创建
        const stage_preload="preloading";//预加载中
        const stage_ready="ready";//预加载完成

        if(pageManager==null || pageManager.stage!=stage_init)
            return;

        pageManager.stage=stage_preload;
        await pageManager.load(argsObj);
        pageManager.stage=stage_ready;

        return pageManager;
    },
    async ShowPageManager(pageManager,argsObj)
    {
        const stage_init="init";//刚创建
        const stage_preload="preloading";//预加载中
        const stage_ready="ready";//预加载完成
        const stage_show="show";//显示
        const stage_destroy="destroy";//关闭后等待销毁

        //空对象
        //已经显示的状态
        //已经在等待显示
        if(pageManager==null || pageManager.stage==stage_show || pageManager.stage==stage_destroy || pageManager.waitReady)
            return;

        if(pageManager.stage==stage_init || pageManager.stage==stage_ready){
            //没预加载或者预加载完成，直接显示
            pageManager.stage=stage_show;//直接改状态，等show回调太晚了
            pageManager.show(argsObj);
        }else if(pageManager.stage==stage_preload){
            pageManager.waitReady=true;
            //等到 pageManager.stage==stage_ready 为止
            //加 pageManager.waitReady 是为了报错时取消不停的等待
            while(pageManager.stage!=stage_ready && pageManager.waitReady)
            {
                await new Promise(resolve => setTimeout(resolve, 100));
            }
            if(pageManager.waitReady){
                pageManager.stage=stage_show;
                pageManager.show(argsObj);
            }
        }

    },
    ShowCPSPageManager(str_tag,str_id,left,top,vertical)
    {
        window.WXWASMSDK.GetOrCreatePageManager(str_tag, (pageManager) => {
            console.log("ShowCPSPageManager ", str_id, "left:", left, "top:", top, "vertical:", vertical);
            pageManager.on(
                'ready', // show | destroy | error
                () => {
                    console.log("ShowCPSPageManager ready ", str_id);
                    moduleHelper.send(
                        'OnReadyCPSPageManagerCallback',
                        str_id,
                    );
                }
            );
            pageManager.on(
                'show', // show | destroy | error
                () => {
                    //console.log("ShowCPSPageManager show ", str_id);
                    moduleHelper.send(
                        'OnShowCPSPageManagerCallback',
                        str_id,
                    );
                }
            );
            pageManager.on(
                'clickgame', // show | destroy | error
                (res) => {
                    //console.log("ShowCPSPageManager clickgame ", str_id, "res:", JSON.stringify(res));
                    res = res == undefined ? {} : res;
                    moduleHelper.send(
                        'OnClickGameCPSPageManagerCallback',
                        JSON.stringify(
                            {
                                id: str_id,
                                targetGameName: res.targetGameName,
                            }),
                    );
                }
            );
            pageManager.on(
                'destroy', // show | destroy | error
                () => {
                    console.log("ShowCPSPageManager destroy ", str_id);
                    moduleHelper.send(
                        'OnDestroyGameCPSPageManagerCallback',
                        str_id,
                    );
                }
            );
            pageManager.on(
                'error', // show | destroy | error
                (res) => {
                    console.log("ShowCPSPageManager error ", JSON.stringify(res));
                    moduleHelper.send(
                        'OnErrorGameCPSPageManagerCallback',
                        JSON.stringify(res),
                    );
                }
            );
            pageManager.show({
                // CPS推荐组件OPENLINK常量，直接复制即可
                openlink: 'wFFX1cDJnwJCet72QGUJJvBpa9z9lfAob-7EYHwzFENHJ_tNECj5LquvJqnbm82RktAcRyg7gORaUSh0yRSiuYF21JvF84j7-SgazajvTW-ScbwFiQccq8FWsrzHVPox1dr90HHv_CTrgRJD4HOdiRJFeLNRrDu0Pj3vsIGuonI',
                query: {
                    id: str_id, // mp端配置的组件id，必填，在 mp 申请

                    //组件的位置，选填，不填时系统自动适应
                    left: left, // 组件左上角屏幕横坐标，单游戏组件/多游戏组件时有效，
                    top: top, // 组件左上角屏幕纵坐标，单游戏组件/多游戏组件时有效
                    isVertical: vertical == 1, // 可选，多游戏组件时有效，默认为false。
                }
            });
        });
    },
    WXChallengeIsValid:function()
    {
        if(wx.getRankManager == undefined)
        {
            return false;
        }
        return true;
    },
    WXUpdateScore:function(scoreKey,score)
    {
        if(wx.getRankManager == undefined)
        {
            return;
        }
        const rankManager = wx.getRankManager();
        rankManager.update({
            scoreKey: scoreKey, // 在MP配置的scoreKey
            score: score, // 具体分数值
            success: res => {
                console.log('分数上报成功', res);
                moduleHelper.send(
                    'OnWXUpdateScoreCallback',
                    JSON.stringify({
                        code: 0
                    })
                );
            },
            fail: err => {
                console.error('分数上报失败', err);
                moduleHelper.send(
                    'OnWXUpdateScoreCallback',
                    JSON.stringify({
                        code: -1,
                        errMsg: "failed",
                    })
                );
            },
        });
    },
    WXCreateChallenge:function(scoreKey)
    {
        if(wx.getRankManager == undefined)
        {
            return;
        }
        wx.getRankManager().createChallenge({
            scoreKey: scoreKey,
            success: res => {
            console.log('擂台赛创建成功', res);
            moduleHelper.send(
                'OnWXCreateChallengeCallback',
                JSON.stringify({
                    code: 0
                })
            );
            },
            fail: err => {
            console.error('擂台赛创建失败', err);
            moduleHelper.send(
                'OnWXCreateChallengeCallback',
                JSON.stringify({
                    code: -1,
                    errMsg: "failed",
                })
            );
            },
          });
    },
    WXEndChallenge:function(scoreKey,score)
    {
        if(wx.getRankManager == undefined)
        {
            return;
        }
        // 结束擂台赛并上报最终分数
        wx.getRankManager().update({
            scoreKey: scoreKey,
            score: score, // 最终分数
            success: res => {
                console.log('擂台赛结束', res);
                moduleHelper.send(
                    'OnWXEndChallengeCallback',
                    JSON.stringify({
                        code: 0
                    })
                );
            },
            fail: err => {
                console.error('擂台赛结束异常', err);
                moduleHelper.send(
                    'OnWXEndChallengeCallback',
                    JSON.stringify({
                        code: -1,
                        errMsg: "failed",
                    })
                );
            },
        });
    },
    WXOnChallengeStart:function()
    {
        if(wx.getRankManager == undefined)
        {
            return;
        }
        // 监听擂台赛开始事件
        wx.getRankManager().onChallengeStart((challengeInfo) => {
            // 处理挑战开始逻辑
            console.log('擂台赛开始', challengeInfo.scoreKey);
            moduleHelper.send(
                'OnWXOnChallengeStartCallback',
                JSON.stringify({
                    code: 0,
                    scoreKey: challengeInfo.scoreKey,
                })
            );
        });
    },
    WXChallengeMiddleUpdate:function(scoreKey,score)
    {
        if(wx.getRankManager == undefined)
        {
            return;
        }
        const rankManager = wx.getRankManager()
        rankManager.middleUpdate({
            scoreKey: scoreKey, // 在 MP后台-运营功能管理-基础配置-游戏玩法ID 中配置
            score: score,
            success: () => {
                console.log('分数更新成功')
                moduleHelper.send(
                    'OnWXChallengeMiddleUpdateCallback',
                    JSON.stringify({
                        code: 0,
                    })
                );
            },
            fail: (err) => {
                console.error('分数更新失败', err)
                moduleHelper.send(
                    'OnWXChallengeMiddleUpdateCallback',
                    JSON.stringify({
                        code: -1,
                        errMsg: "failed",
                    })
                );
            }
        })
    },
    WXQuitChallenge:function()
    {
        if(wx.getRankManager == undefined)
        {
            return;
        }
        wx.getRankManager().abort({
            success: (res) => {
            console.log('擂台赛退出成功', res);
            moduleHelper.send(
                'OnWXQuitChallengeCallback',
                JSON.stringify({
                    code: 0,
                })
            );
            },
            fail: (err) => {
            console.error('擂台赛退出失败', err);
            moduleHelper.send(
                'OnWXQuitChallengeCallback',
                JSON.stringify({
                    code: -1,
                    errMsg: "failed",
                })
            );
            },
          });
    },
    WXGetDirectAdStatus:function()
    {
        if(wx.getDirectAdStatusSync == undefined)
        {
            console.log("WX_FeedPlay WXGetDirectAdStatus,undefined");
            return;
        }
        // 先判断场景值
        const launchOptions = wx.getLaunchOptionsSync();
        if (launchOptions) {

            let isDirectPlay = false;
            if(launchOptions.scene === 1387)
            {
                isDirectPlay = true;
            }
            else if(launchOptions.query != null && launchOptions.query.minigame_direct_debug === "true")
            {
                isDirectPlay = true;
            }
            if (isDirectPlay) {

                const statusInfo = wx.getDirectAdStatusSync();
                console.log("WX_FeedPlay js Status 当前是否在蒙层阶段:" + statusInfo.isInMask);
                console.log("WX_FeedPlay js Status当前是否在直玩广告中:" + statusInfo.isInDirectGameAd);
                moduleHelper.send(
                    'OnWXOnDirectAdStatusChangeCallback',
                    JSON.stringify({
                        isInMask: statusInfo.isInMask,
                        isInDirectGameAd: statusInfo.isInDirectGameAd
                    })
                );
            }
        }
    },
    WXOnDirectAdStatusChange:function()
    {
        if(wx.onDirectAdStatusChange == undefined)
        {
            console.log("WX_FeedPlay WXOnDirectAdStatusChange,undefined");
            return;
        }
        const launchOptions = wx.getLaunchOptionsSync();
        if (launchOptions)
        {
            let isDirectPlay = false;
            if(launchOptions.scene === 1387)
            {
                isDirectPlay = true;
            }
            else if(launchOptions.query != null && launchOptions.query.minigame_direct_debug === "true")
            {
                isDirectPlay = true;
            }

            if(isDirectPlay)
            {
                wx.onDirectAdStatusChange(res => {
                    // 会有如下的几种状态值组合
                    // a) { isInMask: true, isInDirectGameAd: true } -> 表示当前正在直玩广告 且 未戳破蒙层
                    // b) { isInMask: false, isInDirectGameAd: true } -> 表示当前正在直玩广告 且 戳破了蒙层
                    // c) { isInMask: false, isInDirectGameAd: false, isEndByAbnormal: false }, -> 表示倒计时结束了，并且选择了继续玩
                    // d) { isInMask: false, isInDirectGameAd: false, isEndByAbnormal: true }, -> 表示由于异常流程而结束
                    console.log("WX_FeedPlay js Status 当前是否在蒙层阶段:" + res.isInMask)
                    console.log("WX_FeedPlay js Status当前是否在直玩广告中:" + res.isInDirectGameAd)
                    console.log("WX_FeedPlay js Status当前是否由于异常流程而结束:" + res.isEndByAbnormal)
                    moduleHelper.send(
                        'OnWXOnDirectAdStatusChangeCallback',
                        JSON.stringify({
                            isInMask: res.isInMask,
                            isInDirectGameAd: res.isInDirectGameAd,
                            isEndByAbnormal: res.isEndByAbnormal,
                        })
                    );
                  })
            }
        }
    },
    WXCheckIsSupportMidasPayment: function() {
        if (wx.checkIsSupportMidasPayment) {
            wx.checkIsSupportMidasPayment({
                success(res) {
                    console.log('支持检查结果:', res);
                    var allowPay = res.data && res.data.allow_pay;
                    if (allowPay) {
                        console.log('当前环境支持支付');
                    } else {
                        console.log('当前环境不支持支付');
                    }
                    moduleHelper.send(
                        'OnWXCheckIsSupportMidasPaymentCallback',
                        JSON.stringify({
                            code: 0,
                            support: !!allowPay,
                        })
                    );
                },
                fail(err) {
                    console.error('检查支持情况失败', err);
                    moduleHelper.send(
                        'OnWXCheckIsSupportMidasPaymentCallback',
                        JSON.stringify({
                            code: -1,
                            errMsg: err && err.errMsg ? err.errMsg : "failed",
                        })
                    );
                },
                complete() {
                    console.log('检查完成');
                }
            });
        } else {
            // 方法不存在，认为不支持
            moduleHelper.send(
                'OnWXCheckIsSupportMidasPaymentCallback',
                JSON.stringify({
                    code: -1,
                    errMsg: "API not available",
                })
            );
        }
    }
    
}