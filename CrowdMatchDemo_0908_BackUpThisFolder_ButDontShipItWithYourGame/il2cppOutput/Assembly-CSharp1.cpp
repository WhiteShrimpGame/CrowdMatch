#include "pch-cpp.hpp"

#ifndef _MSC_VER
# include <alloca.h>
#else
# include <malloc.h>
#endif


#include <limits>
#include <stdint.h>


template <typename R>
struct VirtualFuncInvoker0
{
	typedef R (*Func)(void*, const RuntimeMethod*);

	static inline R Invoke (Il2CppMethodSlot slot, RuntimeObject* obj)
	{
		const VirtualInvokeData& invokeData = il2cpp_codegen_get_virtual_invoke_data(slot, obj);
		return ((Func)invokeData.methodPtr)(obj, invokeData.method);
	}
};
struct InterfaceActionInvoker0
{
	typedef void (*Action)(void*, const RuntimeMethod*);

	static inline void Invoke (Il2CppMethodSlot slot, RuntimeClass* declaringInterface, RuntimeObject* obj)
	{
		const VirtualInvokeData& invokeData = il2cpp_codegen_get_interface_invoke_data(slot, obj, declaringInterface);
		((Action)invokeData.methodPtr)(obj, invokeData.method);
	}
};
template <typename R>
struct InterfaceFuncInvoker0
{
	typedef R (*Func)(void*, const RuntimeMethod*);

	static inline R Invoke (Il2CppMethodSlot slot, RuntimeClass* declaringInterface, RuntimeObject* obj)
	{
		const VirtualInvokeData& invokeData = il2cpp_codegen_get_interface_invoke_data(slot, obj, declaringInterface);
		return ((Func)invokeData.methodPtr)(obj, invokeData.method);
	}
};
template <typename R, typename T1>
struct InterfaceFuncInvoker1
{
	typedef R (*Func)(void*, T1, const RuntimeMethod*);

	static inline R Invoke (Il2CppMethodSlot slot, RuntimeClass* declaringInterface, RuntimeObject* obj, T1 p1)
	{
		const VirtualInvokeData& invokeData = il2cpp_codegen_get_interface_invoke_data(slot, obj, declaringInterface);
		return ((Func)invokeData.methodPtr)(obj, p1, invokeData.method);
	}
};

// DG.Tweening.Plugins.Core.ABSTweenPlugin`3<UnityEngine.Quaternion,UnityEngine.Vector3,DG.Tweening.Plugins.Options.QuaternionOptions>
struct ABSTweenPlugin_3_t3823C0F935A3168B9E48DC90ABD9A0CED3D7BB82;
// DG.Tweening.Plugins.Core.ABSTweenPlugin`3<UnityEngine.Vector3,UnityEngine.Vector3,DG.Tweening.Plugins.Options.VectorOptions>
struct ABSTweenPlugin_3_tE5A78BE46D046C07A6356B8AB596B2D00F9295E7;
// DG.Tweening.Core.DOGetter`1<UnityEngine.Quaternion>
struct DOGetter_1_tB89DD12456B8E79576BB70E1CA6DF899686410D3;
// DG.Tweening.Core.DOGetter`1<UnityEngine.Vector3>
struct DOGetter_1_t709462C08281F3AA5DFEF36CAF91404B1004C338;
// DG.Tweening.Core.DOSetter`1<UnityEngine.Quaternion>
struct DOSetter_1_t9EFF8DD70A15F455A6FE698A22BD0FE9683AC28E;
// DG.Tweening.Core.DOSetter`1<UnityEngine.Vector3>
struct DOSetter_1_t02E8F9920F174322F1CF5AC8BCDEAABD14A03358;
// System.Func`3<System.Int32,System.Int32,System.Nullable`1<UnityEngine.Color>>
struct Func_3_t171A20B65D1078C91C9A875EF6CBB2716C7E322D;
// System.Func`3<System.Int32,System.Int32,System.Boolean>
struct Func_3_tE8F85DA3CAC4998201E5C56356280AFAB7185B69;
// System.Collections.Generic.HashSet`1<UnityEngine.Vector2Int>
struct HashSet_1_t55B946E0B889BDE32B3004D0822A6C5017AFA406;
// System.Collections.Generic.IEnumerable`1<UnityEngine.Vector2>
struct IEnumerable_1_t6C47A8FE62321E6AD75C312B8549AFD2B13F0591;
// System.Collections.Generic.IEnumerable`1<UnityEngine.Vector2Int>
struct IEnumerable_1_t8845214D7CADFAAD7AB98132A368905184A79DDF;
// System.Collections.Generic.IEnumerator`1<CrowdMatch.PixelItem>
struct IEnumerator_1_tBDD606A60DDAC54AA8C9FED0A6896689D4639327;
// System.Collections.Generic.IEqualityComparer`1<UnityEngine.Vector2Int>
struct IEqualityComparer_1_t4275A3D7B86C2D3C66842AB0700881FB24837F2D;
// System.Collections.Generic.IList`1<UnityEngine.Vector2>
struct IList_1_t0DF1E5F56EE58E1A7F1FE26A676FC9FBF4D52A07;
// System.Collections.Generic.IReadOnlyList`1<UnityEngine.Vector2>
struct IReadOnlyList_1_t3067BC0A09F7D5ADBABEE74BCB8640FACCBC19A0;
// System.Collections.Generic.List`1<System.ValueTuple`3<System.Int32,System.Int32,System.Int32>>
struct List_1_t264DA9C475A0E3CF57148457209560CF2E2B6F27;
// System.Collections.Generic.List`1<UnityEngine.CanvasGroup>
struct List_1_t2CDCA768E7F493F5EDEBC75AEB200FD621354E35;
// System.Collections.Generic.List`1<System.Object>
struct List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D;
// System.Collections.Generic.List`1<CrowdMatch.PixelItem>
struct List_1_tB8E48F8C3B88C3DD79D5A515A2A033D8CD5FEAD6;
// System.Collections.Generic.List`1<UnityEngine.Renderer>
struct List_1_tD435DCC2A88E36DFC551EA5392CE0182F0C50E93;
// System.Collections.Generic.List`1<UnityEngine.TextAsset>
struct List_1_tC0FCC010411366A6623886AFA93A0B022E62D015;
// System.Collections.Generic.List`1<UnityEngine.Vector2>
struct List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B;
// System.Collections.Generic.List`1<UnityEngine.Vector2Int>
struct List_1_tB56F1028A724D2CE4E84861619D1CF68C68C983D;
// System.Collections.Generic.Queue`1<UnityEngine.Vector2Int>
struct Queue_1_t9B96DF949ACABD4B0F595738FC87DC80BCCC3875;
// DG.Tweening.TweenCallback`1<System.Int32>
struct TweenCallback_1_tF0ADCA0C226C9C243ACB55E67D852E4BB53AEB67;
// DG.Tweening.Core.TweenerCore`3<UnityEngine.Quaternion,UnityEngine.Vector3,DG.Tweening.Plugins.Options.QuaternionOptions>
struct TweenerCore_3_t392C54729BB024F5802F8E205C583653C4E886E3;
// DG.Tweening.Core.TweenerCore`3<UnityEngine.Vector3,UnityEngine.Vector3,DG.Tweening.Plugins.Options.VectorOptions>
struct TweenerCore_3_tCD82DFC45FB71C681FA8659EA63A7D7D16BFFE77;
// System.Collections.Generic.HashSet`1/Slot<UnityEngine.Vector2Int>[]
struct SlotU5BU5D_t893ED63C996E15799645D3C23B6BAFD24BD5E36F;
// System.ValueTuple`3<System.Int32,System.Int32,System.Int32>[]
struct ValueTuple_3U5BU5D_t2EE684CE383C822F20AF09A2B0A3C10C5A8A94E5;
// UnityEngine.Color[]
struct ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389;
// System.Delegate[]
struct DelegateU5BU5D_tC5AB7E8F745616680F337909D3A8E6C722CDF771;
// System.Int32[]
struct Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C;
// System.IntPtr[]
struct IntPtrU5BU5D_tFD177F8C806A6921AD7150264CCC62FA00CAD832;
// UnityEngine.Material[]
struct MaterialU5BU5D_t2B1D11C42DB07A4400C0535F92DBB87A2E346D3D;
// System.Object[]
struct ObjectU5BU5D_t8061030B0A12A55D5AD8652A20C922FE99450918;
// CrowdMatch.PixelItem[]
struct PixelItemU5BU5D_t48D4473FA8D417437EB43513F73EC0FD24CBD02F;
// UnityEngine.Renderer[]
struct RendererU5BU5D_t32FDD782F67917B2291EA4FF242719877440A02A;
// UnityEngine.UI.Selectable[]
struct SelectableU5BU5D_t4160E135F02A40F75A63F787D36F31FEC6FE91A9;
// System.Diagnostics.StackTrace[]
struct StackTraceU5BU5D_t32FBCB20930EAF5BAE3F450FF75228E5450DA0DF;
// System.String[]
struct StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248;
// UnityEngine.Vector2[]
struct Vector2U5BU5D_tFEBBC94BCC6C9C88277BA04047D2B3FDB6ED7FDA;
// UnityEngine.Vector2Int[]
struct Vector2IntU5BU5D_tF9E2BDAC11B246DF7EEB9137B826A0CBEBD59534;
// CrowdMatch.WallItem[]
struct WallItemU5BU5D_tD1B1EF0B4FAE8376B92DB7B23ACCE192653C9B58;
// System.Boolean[,]
struct BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6;
// CrowdMatch.PixelItem[,]
struct PixelItemU5BU2CU5D_t26ACEBF9031979698453CA52C61A20C0A1A68C7F;
// UnityEngine.UI.AnimationTriggers
struct AnimationTriggers_tA0DC06F89C5280C6DD972F6F4C8A56D7F4F79074;
// UnityEngine.Animator
struct Animator_t8A52E42AE54F76681838FE9E632683EF3952E883;
// System.AsyncCallback
struct AsyncCallback_t7FEF460CBDCFB9C5FA2EF776984778B9A4145F4C;
// UnityEngine.Behaviour
struct Behaviour_t01970CFBBA658497AE30F311C447DB0440BAB7FA;
// UnityEngine.BoxCollider
struct BoxCollider_tFA5D239388334D6DE0B8FFDAD6825C5B03786E23;
// UnityEngine.UI.Button
struct Button_t6786514A57F7AFDEE5431112FEA0CAB24F5AE098;
// UnityEngine.Collider
struct Collider_t1CC3163924FCD6C4CC2E816373A929C1E3D55E76;
// CrowdMatch.ColorConfig
struct ColorConfig_tCFE08A98D99EE62711B453124E88292D1545172C;
// UnityEngine.Component
struct Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3;
// CrowdMatch.ContainerGroup
struct ContainerGroup_t4E8C2C211C9490625D25CE060D3AD94505CA9300;
// CrowdMatch.ConveyorBeltZone
struct ConveyorBeltZone_t02C9C7082253896FBB1D33FDCF4897E1865FA3DB;
// UnityEngine.Coroutine
struct Coroutine_t85EA685566A254C23F3FD77AB5BDFFFF8799596B;
// CrowdMatch.CrowdBufferZone
struct CrowdBufferZone_t19D1AFC53A6064BCB80182E56B0DAFAA95723CBD;
// System.DelegateData
struct DelegateData_t9B286B493293CD2D23A5B2B5EF0E5B1324C2B77E;
// DG.Tweening.EaseFunction
struct EaseFunction_t0F945D9D726B0915C5FBF30862E987EC3AC12A04;
// CrowdMatch.GameController
struct GameController_t9B394943D9DA551993B8515B21F692D9B0E00853;
// CrowdMatch.GameManager
struct GameManager_t5B23045F478AD8B0E3D943ECC8493384B02FF32F;
// UnityEngine.GameObject
struct GameObject_t76FEDD663AB33C991A9C9A23129337651094216F;
// UnityEngine.UI.Graphic
struct Graphic_tCBFCA4585A19E2B75465AECFEAC43F4016BF7931;
// System.IAsyncResult
struct IAsyncResult_t7B9B5A0ECB35DCEC31B8A8122C37D687369253B5;
// System.Collections.IDictionary
struct IDictionary_t6D03155AF1FA9083817AA5B6AD7DEEACC26AB220;
// System.Collections.IEnumerator
struct IEnumerator_t7B609C2FFA6EB5167D9C62A0C32A21DE2F666DAA;
// UnityEngine.Events.InvokableCallList
struct InvokableCallList_t309E1C8C7CE885A0D2F98C84CEA77A8935688382;
// CrowdMatch.LevelDataConfig
struct LevelDataConfig_t640D26E9446ECE825FF36A677A524E81EC855513;
// CrowdMatch.LevelSkipButtons
struct LevelSkipButtons_t116E0448EB9F81B180CA7AA70DCB08A06728621D;
// UnityEngine.Material
struct Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3;
// System.Reflection.MethodInfo
struct MethodInfo_t;
// UnityEngine.MonoBehaviour
struct MonoBehaviour_t532A11E69716D348D8AA7F854AFCBFCB8AD17F71;
// System.NotSupportedException
struct NotSupportedException_t1429765983D409BD2986508963C98D214E4EBF4A;
// UnityEngine.Object
struct Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C;
// UnityEngine.Events.PersistentCallGroup
struct PersistentCallGroup_tB826EDF15DC80F71BCBCD8E410FD959A04C33F25;
// CrowdMatch.PixelClickListener
struct PixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524;
// CrowdMatch.PixelGroup
struct PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80;
// CrowdMatch.PixelItem
struct PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E;
// UnityEngine.Renderer
struct Renderer_t320575F223BCB177A982E5DDB5DB19FAA89E7FBF;
// System.Runtime.Serialization.SafeSerializationManager
struct SafeSerializationManager_tCBB85B95DFD1634237140CD892E82D06ECB3F5E6;
// UnityEngine.UI.Selectable
struct Selectable_t3251808068A17B8E92FB33590A4C2FA66D456712;
// DG.Tweening.Sequence
struct Sequence_tEADBE56D6ED2E9EE8FB2E5459C3E57131EC0545C;
// System.Runtime.Serialization.SerializationInfo
struct SerializationInfo_t3C47F63E24BEB9FCE2DC6309E027F238DC5C5E37;
// UnityEngine.Sprite
struct Sprite_tAFF74BC83CD68037494CB0B4F28CBDF8971CAB99;
// System.IO.StreamWriter
struct StreamWriter_t6E7DF7D524AA3C018A65F62EE80779873ED4D1E4;
// System.String
struct String_t;
// UnityEngine.UI.Text
struct Text_tD60B2346DAA6666BF0D822FF607F0B220C2B9E62;
// UnityEngine.Texture2D
struct Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4;
// UnityEngine.Transform
struct Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1;
// DG.Tweening.TweenCallback
struct TweenCallback_t7C8B8A38E7B30905FF1B83C943256EF23617BB24;
// System.Type
struct Type_t;
// UnityEngine.Events.UnityAction
struct UnityAction_t11A1F3B953B365C072A5DCC32677EE1796A962A7;
// UnityEngine.Events.UnityEvent
struct UnityEvent_tDC2C3548799DBC91D1E3F3DE60083A66F4751977;
// System.Void
struct Void_t4861ACF8F4594C3437BB48B6E56783494B843915;
// CrowdMatch.WallItem
struct WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0;
// UnityEngine.UI.Button/ButtonClickedEvent
struct ButtonClickedEvent_t8EA72E90B3BD1392FB3B3EF167D5121C23569E4C;
// CrowdMatch.GameController/<GetNeighbors>d__43
struct U3CGetNeighborsU3Ed__43_t7EE77D676072170DE792051688D0316271D3643C;
// CrowdMatch.GameController/<MoveToGatherPoint>d__45
struct U3CMoveToGatherPointU3Ed__45_t540C804CD76258EA246336B338FC828D346240AF;
// CrowdMatch.PixelItem/<MoveExposeTargetToY>d__35
struct U3CMoveExposeTargetToYU3Ed__35_tAD80531ED95A191AB90EE89508DFC17246399033;

IL2CPP_EXTERN_C RuntimeClass* BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Debug_t8394C7EEAECA3689C2C9B9DE9C7166D73596276F_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* GameManager_t5B23045F478AD8B0E3D943ECC8493384B02FF32F_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* GameObject_t76FEDD663AB33C991A9C9A23129337651094216F_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* HashSet_1_t55B946E0B889BDE32B3004D0822A6C5017AFA406_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* IEnumerable_1_t8845214D7CADFAAD7AB98132A368905184A79DDF_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* IEnumerator_1_t772680255A1A75379853D3823763B88BF026E055_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* IEnumerator_t7B609C2FFA6EB5167D9C62A0C32A21DE2F666DAA_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* IReadOnlyCollection_1_t846EB7065157C69B6F0123614957234D9A0AC8D9_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* IReadOnlyList_1_t3067BC0A09F7D5ADBABEE74BCB8640FACCBC19A0_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* List_1_t264DA9C475A0E3CF57148457209560CF2E2B6F27_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* List_1_tB56F1028A724D2CE4E84861619D1CF68C68C983D_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* List_1_tD435DCC2A88E36DFC551EA5392CE0182F0C50E93_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Mathf_tE284D016E3B297B72311AAD9EB8F0E643F6A4682_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* NotSupportedException_t1429765983D409BD2986508963C98D214E4EBF4A_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* PixelItemU5BU2CU5D_t26ACEBF9031979698453CA52C61A20C0A1A68C7F_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Queue_1_t9B96DF949ACABD4B0F595738FC87DC80BCCC3875_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* TweenCallback_t7C8B8A38E7B30905FF1B83C943256EF23617BB24_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* U3CGetNeighborsU3Ed__43_t7EE77D676072170DE792051688D0316271D3643C_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* U3CMoveExposeTargetToYU3Ed__35_tAD80531ED95A191AB90EE89508DFC17246399033_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* UnityAction_t11A1F3B953B365C072A5DCC32677EE1796A962A7_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeField* U3CPrivateImplementationDetailsU3E_t0F5473E849A5A5185A9F4C5246F0C32816C49FCA____CD9A54ED1F18BF97DB08914E280EA7349E11CA2C4885A4D8052552CEBA84208D_0_FieldInfo_var;
IL2CPP_EXTERN_C String_t* _stringLiteral12326FAECC370AC4E02673E03510901F14A2F650;
IL2CPP_EXTERN_C String_t* _stringLiteral1E7C5AC3F45D7F269BDBDB108C7B98958BBA63CD;
IL2CPP_EXTERN_C String_t* _stringLiteral2FB2883B7696560B5CEC12690102A0B2BE1338A9;
IL2CPP_EXTERN_C String_t* _stringLiteral50639CAD49418C7B223CC529395C0E2A3892501C;
IL2CPP_EXTERN_C String_t* _stringLiteral562761ED06D88AFEA8594A1A352122119A54DAC9;
IL2CPP_EXTERN_C String_t* _stringLiteral6B8C303E7710B6904B487592D91528D80F4548B2;
IL2CPP_EXTERN_C String_t* _stringLiteral6F9A0883243199EB61F492C47190A43C05BCED11;
IL2CPP_EXTERN_C String_t* _stringLiteral73D9C88DD061503C8E495188F237E1901308C684;
IL2CPP_EXTERN_C String_t* _stringLiteral7F4682F559107108074468C96021CDFA3B5C0C04;
IL2CPP_EXTERN_C String_t* _stringLiteral90F54B7BFEE63FC7CF1A6ECC3EBE9DEFA4807B5E;
IL2CPP_EXTERN_C String_t* _stringLiteralC142EFACE770062722ED88219F1024199495E0EC;
IL2CPP_EXTERN_C String_t* _stringLiteralD36070345E1BBE825940C76A43B5AD5F33F3FC62;
IL2CPP_EXTERN_C String_t* _stringLiteralEDE2495E1435E5A58A340546D3AB772C2D72C193;
IL2CPP_EXTERN_C String_t* _stringLiteralF8511B43E7BE40B97DDDA39366F04722017C7098;
IL2CPP_EXTERN_C String_t* _stringLiteralFCC0421EA35E2D4A6C63BB7E474C10CA9BC2EB7B;
IL2CPP_EXTERN_C const RuntimeMethod* Component_GetComponentInChildren_TisPixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524_m373C622F45C151B1A0749FD577AFDC09F0488041_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Component_GetComponentInParent_TisPixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80_mD60D9A2DE017B170590FDF6B2CB4CB66A737CD4F_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Component_GetComponent_TisBoxCollider_tFA5D239388334D6DE0B8FFDAD6825C5B03786E23_m59698092F1230C6FB7F40D0F58F643A931A732D7_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Component_GetComponentsInChildren_TisPixelItem_t863890C77945A8C08435BF2F8859A47A413C794E_mC6DFA6AEA610A79B3113B66C8457AA3325D29586_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Component_GetComponentsInChildren_TisWallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0_m9AD00368CDD148B6F031F3195DAF4512127EBA59_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Enumerator_Dispose_m2A96F62698864FA1E73292450EBF2019F7104BBC_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Enumerator_Dispose_m39794B37E9AE88ED22C03824DE8D637C0DADBAF0_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Enumerator_MoveNext_m36545FD9B7C5DA66EAD80FD8813D279003BA8749_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Enumerator_MoveNext_mD6D16710D40F62D081A4973E4D8CA1614D1482B5_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Enumerator_get_Current_m87245A61572727EBCD1642C4A2BD99B11CE9FA8A_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Enumerator_get_Current_mF219FEB0F2097ED593A4E6E2283167335AFBA4B6_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* GameObject_AddComponent_TisWallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0_mF275F42400D186788F6E0B363E9F0D081AB0FD8A_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* GameObject_GetComponent_TisButton_t6786514A57F7AFDEE5431112FEA0CAB24F5AE098_mB997CBF78A37938DC1624352E12D0205078CB290_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* GameObject_GetComponent_TisPixelItem_t863890C77945A8C08435BF2F8859A47A413C794E_m7C12F9FAB885712E9777CD52832D09C1C0C4989B_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* HashSet_1_Add_m517B9238E386530A854B4286891358CC8327A7B6_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* HashSet_1__ctor_m0E27874668BB3B3160062D69799276CFEF8072AB_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* LevelSkipButtons_NextLevel_m6BC706B486985644D627D363EE506DBBDB9E4C78_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* LevelSkipButtons_PrevLevel_m7734B71E7CF419EF9F4AE420091E2F57A52A00AC_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* List_1_Add_m716377944B1E88CBB1B269AA9CF38C525A41D367_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* List_1_Add_m771AC7A01DFC931CCCFCCF949C1F4D56B5E98A1B_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* List_1_GetEnumerator_m039302BD172C3288503DB73B6E2B27B8D8BC0321_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* List_1_GetEnumerator_mC86A1EF9E784B7E7B5C00025383C6381B831F88C_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* List_1__ctor_m105596C2159C46B75E96D26ACEC0A5C1C1F5C5EC_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* List_1__ctor_m7734609AB0CE32B017F3FBC89E3A0D35323A5015_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* List_1__ctor_m803E10F7A50EB22BF82C0C1AB251D5407B4496DE_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* List_1__ctor_m88C4BD8AC607DB3585552068F4DC437406358D5F_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* List_1__ctor_mE1D9FD9DA1EF2CAC4F99EF4E013F05BB8C3507EF_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* List_1_get_Count_mF1C0C56457C655BDFFC6EE5B46FAD8BAEC1F588B_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* List_1_get_Item_m1F8E226CAD72B83C5E75BB66B43025247806B543_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Nullable_1_GetValueOrDefault_m9A7869C021F041D45F2A851F70F97F8114AC99E4_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Nullable_1_get_HasValue_mFAF0B4EEA878E596C80258FE3BDA57CEF40C8D7F_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Nullable_1_get_Value_m3FC15B40E747AA6A9F2AB10A00C46ABE84393637_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Object_Instantiate_TisGameObject_t76FEDD663AB33C991A9C9A23129337651094216F_m10D87C6E0708CA912BBB02555BF7D0FBC5D7A2B3_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* PixelItem_OnIdleSmoothComplete_mE4C6BF6BD71AD3B54E824F9C4C4844FECB9F8F99_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Queue_1_Dequeue_mD30377AD154F6A542F280578B393102A1D5378EB_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Queue_1_Enqueue_m8507576A07092ADD61289B4C7F248A6C94944EFB_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Queue_1__ctor_m262194FC7D76E5DB95E022130A30E64125C0A90E_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* Queue_1_get_Count_m1FE2DD00C23DF83E6DBC5BE6A23A9FE7FBF772DD_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* TweenSettingsExtensions_OnComplete_TisTweenerCore_3_t392C54729BB024F5802F8E205C583653C4E886E3_m8BE213B05FF94E9AE889B180DB8B3DE6EC4EB1E1_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* U3CGetNeighborsU3Ed__43_System_Collections_IEnumerator_Reset_mD3F0A59DA69CFA678E7F7F98A2E9C717B550A935_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* U3CMoveExposeTargetToYU3Ed__35_System_Collections_IEnumerator_Reset_mF4D2214C9CA9CABDC11398D89EE5351A6C7FDA02_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* U3CMoveToGatherPointU3Ed__45_System_Collections_IEnumerator_Reset_m3579E49E6A5A1582EDE1A9027914E9A69863DC76_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* ValueTuple_3__ctor_m0D7E698F23721325245996D0B8DED1C102559F60_RuntimeMethod_var;
struct Delegate_t_marshaled_com;
struct Delegate_t_marshaled_pinvoke;
struct Exception_t_marshaled_com;
struct Exception_t_marshaled_pinvoke;

struct ValueTuple_3U5BU5D_t2EE684CE383C822F20AF09A2B0A3C10C5A8A94E5;
struct ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389;
struct Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C;
struct PixelItemU5BU5D_t48D4473FA8D417437EB43513F73EC0FD24CBD02F;
struct StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248;
struct Vector2IntU5BU5D_tF9E2BDAC11B246DF7EEB9137B826A0CBEBD59534;
struct WallItemU5BU5D_tD1B1EF0B4FAE8376B92DB7B23ACCE192653C9B58;
struct BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6;
struct PixelItemU5BU2CU5D_t26ACEBF9031979698453CA52C61A20C0A1A68C7F;

IL2CPP_EXTERN_C_BEGIN
IL2CPP_EXTERN_C_END

#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif

// System.Collections.Generic.HashSet`1<UnityEngine.Vector2Int>
struct HashSet_1_t55B946E0B889BDE32B3004D0822A6C5017AFA406  : public RuntimeObject
{
	// System.Int32[] System.Collections.Generic.HashSet`1::_buckets
	Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* ____buckets_7;
	// System.Collections.Generic.HashSet`1/Slot<T>[] System.Collections.Generic.HashSet`1::_slots
	SlotU5BU5D_t893ED63C996E15799645D3C23B6BAFD24BD5E36F* ____slots_8;
	// System.Int32 System.Collections.Generic.HashSet`1::_count
	int32_t ____count_9;
	// System.Int32 System.Collections.Generic.HashSet`1::_lastIndex
	int32_t ____lastIndex_10;
	// System.Int32 System.Collections.Generic.HashSet`1::_freeList
	int32_t ____freeList_11;
	// System.Collections.Generic.IEqualityComparer`1<T> System.Collections.Generic.HashSet`1::_comparer
	RuntimeObject* ____comparer_12;
	// System.Int32 System.Collections.Generic.HashSet`1::_version
	int32_t ____version_13;
	// System.Runtime.Serialization.SerializationInfo System.Collections.Generic.HashSet`1::_siInfo
	SerializationInfo_t3C47F63E24BEB9FCE2DC6309E027F238DC5C5E37* ____siInfo_14;
};

// System.Collections.Generic.List`1<System.ValueTuple`3<System.Int32,System.Int32,System.Int32>>
struct List_1_t264DA9C475A0E3CF57148457209560CF2E2B6F27  : public RuntimeObject
{
	// T[] System.Collections.Generic.List`1::_items
	ValueTuple_3U5BU5D_t2EE684CE383C822F20AF09A2B0A3C10C5A8A94E5* ____items_1;
	// System.Int32 System.Collections.Generic.List`1::_size
	int32_t ____size_2;
	// System.Int32 System.Collections.Generic.List`1::_version
	int32_t ____version_3;
	// System.Object System.Collections.Generic.List`1::_syncRoot
	RuntimeObject* ____syncRoot_4;
};

struct List_1_t264DA9C475A0E3CF57148457209560CF2E2B6F27_StaticFields
{
	// T[] System.Collections.Generic.List`1::s_emptyArray
	ValueTuple_3U5BU5D_t2EE684CE383C822F20AF09A2B0A3C10C5A8A94E5* ___s_emptyArray_5;
};

// System.Collections.Generic.List`1<UnityEngine.Renderer>
struct List_1_tD435DCC2A88E36DFC551EA5392CE0182F0C50E93  : public RuntimeObject
{
	// T[] System.Collections.Generic.List`1::_items
	RendererU5BU5D_t32FDD782F67917B2291EA4FF242719877440A02A* ____items_1;
	// System.Int32 System.Collections.Generic.List`1::_size
	int32_t ____size_2;
	// System.Int32 System.Collections.Generic.List`1::_version
	int32_t ____version_3;
	// System.Object System.Collections.Generic.List`1::_syncRoot
	RuntimeObject* ____syncRoot_4;
};

struct List_1_tD435DCC2A88E36DFC551EA5392CE0182F0C50E93_StaticFields
{
	// T[] System.Collections.Generic.List`1::s_emptyArray
	RendererU5BU5D_t32FDD782F67917B2291EA4FF242719877440A02A* ___s_emptyArray_5;
};

// System.Collections.Generic.List`1<UnityEngine.Vector2>
struct List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B  : public RuntimeObject
{
	// T[] System.Collections.Generic.List`1::_items
	Vector2U5BU5D_tFEBBC94BCC6C9C88277BA04047D2B3FDB6ED7FDA* ____items_1;
	// System.Int32 System.Collections.Generic.List`1::_size
	int32_t ____size_2;
	// System.Int32 System.Collections.Generic.List`1::_version
	int32_t ____version_3;
	// System.Object System.Collections.Generic.List`1::_syncRoot
	RuntimeObject* ____syncRoot_4;
};

struct List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B_StaticFields
{
	// T[] System.Collections.Generic.List`1::s_emptyArray
	Vector2U5BU5D_tFEBBC94BCC6C9C88277BA04047D2B3FDB6ED7FDA* ___s_emptyArray_5;
};

// System.Collections.Generic.List`1<UnityEngine.Vector2Int>
struct List_1_tB56F1028A724D2CE4E84861619D1CF68C68C983D  : public RuntimeObject
{
	// T[] System.Collections.Generic.List`1::_items
	Vector2IntU5BU5D_tF9E2BDAC11B246DF7EEB9137B826A0CBEBD59534* ____items_1;
	// System.Int32 System.Collections.Generic.List`1::_size
	int32_t ____size_2;
	// System.Int32 System.Collections.Generic.List`1::_version
	int32_t ____version_3;
	// System.Object System.Collections.Generic.List`1::_syncRoot
	RuntimeObject* ____syncRoot_4;
};

struct List_1_tB56F1028A724D2CE4E84861619D1CF68C68C983D_StaticFields
{
	// T[] System.Collections.Generic.List`1::s_emptyArray
	Vector2IntU5BU5D_tF9E2BDAC11B246DF7EEB9137B826A0CBEBD59534* ___s_emptyArray_5;
};

// System.Collections.Generic.Queue`1<UnityEngine.Vector2Int>
struct Queue_1_t9B96DF949ACABD4B0F595738FC87DC80BCCC3875  : public RuntimeObject
{
	// T[] System.Collections.Generic.Queue`1::_array
	Vector2IntU5BU5D_tF9E2BDAC11B246DF7EEB9137B826A0CBEBD59534* ____array_0;
	// System.Int32 System.Collections.Generic.Queue`1::_head
	int32_t ____head_1;
	// System.Int32 System.Collections.Generic.Queue`1::_tail
	int32_t ____tail_2;
	// System.Int32 System.Collections.Generic.Queue`1::_size
	int32_t ____size_3;
	// System.Int32 System.Collections.Generic.Queue`1::_version
	int32_t ____version_4;
	// System.Object System.Collections.Generic.Queue`1::_syncRoot
	RuntimeObject* ____syncRoot_5;
};

// DG.Tweening.Core.ABSSequentiable
struct ABSSequentiable_t05DF85FC63E3650D2D4CF6ABBA0F43263EB8CE89  : public RuntimeObject
{
	// DG.Tweening.TweenType DG.Tweening.Core.ABSSequentiable::tweenType
	int32_t ___tweenType_0;
	// System.Single DG.Tweening.Core.ABSSequentiable::sequencedPosition
	float ___sequencedPosition_1;
	// System.Single DG.Tweening.Core.ABSSequentiable::sequencedEndPosition
	float ___sequencedEndPosition_2;
	// DG.Tweening.TweenCallback DG.Tweening.Core.ABSSequentiable::onStart
	TweenCallback_t7C8B8A38E7B30905FF1B83C943256EF23617BB24* ___onStart_3;
};
struct Il2CppArrayBounds;

// CrowdMatch.GridColorMatcher
struct GridColorMatcher_t30274A5A8EDBA07A99182F2ED80F1FE4031250AC  : public RuntimeObject
{
};

// CrowdMatch.SquareGridColorTool
struct SquareGridColorTool_tE4AFFCB2D102D6D0EA46BDFB0542911A63BC7AEE  : public RuntimeObject
{
};

// System.String
struct String_t  : public RuntimeObject
{
	// System.Int32 System.String::_stringLength
	int32_t ____stringLength_4;
	// System.Char System.String::_firstChar
	Il2CppChar ____firstChar_5;
};

struct String_t_StaticFields
{
	// System.String System.String::Empty
	String_t* ___Empty_6;
};

// UnityEngine.Events.UnityEventBase
struct UnityEventBase_t4968A4C72559F35C0923E4BD9C042C3A842E1DB8  : public RuntimeObject
{
	// UnityEngine.Events.InvokableCallList UnityEngine.Events.UnityEventBase::m_Calls
	InvokableCallList_t309E1C8C7CE885A0D2F98C84CEA77A8935688382* ___m_Calls_0;
	// UnityEngine.Events.PersistentCallGroup UnityEngine.Events.UnityEventBase::m_PersistentCalls
	PersistentCallGroup_tB826EDF15DC80F71BCBCD8E410FD959A04C33F25* ___m_PersistentCalls_1;
	// System.Boolean UnityEngine.Events.UnityEventBase::m_CallsDirty
	bool ___m_CallsDirty_2;
};

// System.ValueType
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F  : public RuntimeObject
{
};
// Native definition for P/Invoke marshalling of System.ValueType
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F_marshaled_pinvoke
{
};
// Native definition for COM marshalling of System.ValueType
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F_marshaled_com
{
};

// UnityEngine.YieldInstruction
struct YieldInstruction_tFCE35FD0907950EFEE9BC2890AC664E41C53728D  : public RuntimeObject
{
};
// Native definition for P/Invoke marshalling of UnityEngine.YieldInstruction
struct YieldInstruction_tFCE35FD0907950EFEE9BC2890AC664E41C53728D_marshaled_pinvoke
{
};
// Native definition for COM marshalling of UnityEngine.YieldInstruction
struct YieldInstruction_tFCE35FD0907950EFEE9BC2890AC664E41C53728D_marshaled_com
{
};

// CrowdMatch.GameController/<GetNeighbors>d__43
struct U3CGetNeighborsU3Ed__43_t7EE77D676072170DE792051688D0316271D3643C  : public RuntimeObject
{
	// System.Int32 CrowdMatch.GameController/<GetNeighbors>d__43::<>1__state
	int32_t ___U3CU3E1__state_0;
	// CrowdMatch.PixelItem CrowdMatch.GameController/<GetNeighbors>d__43::<>2__current
	PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* ___U3CU3E2__current_1;
	// System.Int32 CrowdMatch.GameController/<GetNeighbors>d__43::<>l__initialThreadId
	int32_t ___U3CU3El__initialThreadId_2;
	// CrowdMatch.GameController CrowdMatch.GameController/<GetNeighbors>d__43::<>4__this
	GameController_t9B394943D9DA551993B8515B21F692D9B0E00853* ___U3CU3E4__this_3;
	// CrowdMatch.PixelItem CrowdMatch.GameController/<GetNeighbors>d__43::item
	PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* ___item_4;
	// CrowdMatch.PixelItem CrowdMatch.GameController/<GetNeighbors>d__43::<>3__item
	PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* ___U3CU3E3__item_5;
	// System.Int32[] CrowdMatch.GameController/<GetNeighbors>d__43::<dx>5__2
	Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* ___U3CdxU3E5__2_6;
	// System.Int32[] CrowdMatch.GameController/<GetNeighbors>d__43::<dz>5__3
	Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* ___U3CdzU3E5__3_7;
	// System.Int32 CrowdMatch.GameController/<GetNeighbors>d__43::<i>5__4
	int32_t ___U3CiU3E5__4_8;
};

// System.Collections.Generic.List`1/Enumerator<System.Object>
struct Enumerator_t9473BAB568A27E2339D48C1F91319E0F6D244D7A 
{
	// System.Collections.Generic.List`1<T> System.Collections.Generic.List`1/Enumerator::_list
	List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D* ____list_0;
	// System.Int32 System.Collections.Generic.List`1/Enumerator::_index
	int32_t ____index_1;
	// System.Int32 System.Collections.Generic.List`1/Enumerator::_version
	int32_t ____version_2;
	// T System.Collections.Generic.List`1/Enumerator::_current
	RuntimeObject* ____current_3;
};

// System.Collections.Generic.List`1/Enumerator<UnityEngine.Renderer>
struct Enumerator_t4B5D90D1324DE6E043169A1E8DCD75512559AFA7 
{
	// System.Collections.Generic.List`1<T> System.Collections.Generic.List`1/Enumerator::_list
	List_1_tD435DCC2A88E36DFC551EA5392CE0182F0C50E93* ____list_0;
	// System.Int32 System.Collections.Generic.List`1/Enumerator::_index
	int32_t ____index_1;
	// System.Int32 System.Collections.Generic.List`1/Enumerator::_version
	int32_t ____version_2;
	// T System.Collections.Generic.List`1/Enumerator::_current
	Renderer_t320575F223BCB177A982E5DDB5DB19FAA89E7FBF* ____current_3;
};

// System.ValueTuple`3<System.Int32,System.Int32,System.Int32>
struct ValueTuple_3_tBFE24970AFB182B3C77F4C747E3F1E7A7B4EBC57 
{
	// T1 System.ValueTuple`3::Item1
	int32_t ___Item1_0;
	// T2 System.ValueTuple`3::Item2
	int32_t ___Item2_1;
	// T3 System.ValueTuple`3::Item3
	int32_t ___Item3_2;
};

// System.Boolean
struct Boolean_t09A6377A54BE2F9E6985A8149F19234FD7DDFE22 
{
	// System.Boolean System.Boolean::m_value
	bool ___m_value_0;
};

struct Boolean_t09A6377A54BE2F9E6985A8149F19234FD7DDFE22_StaticFields
{
	// System.String System.Boolean::TrueString
	String_t* ___TrueString_5;
	// System.String System.Boolean::FalseString
	String_t* ___FalseString_6;
};

// UnityEngine.Color
struct Color_tD001788D726C3A7F1379BEED0260B9591F440C1F 
{
	// System.Single UnityEngine.Color::r
	float ___r_0;
	// System.Single UnityEngine.Color::g
	float ___g_1;
	// System.Single UnityEngine.Color::b
	float ___b_2;
	// System.Single UnityEngine.Color::a
	float ___a_3;
};

// System.Double
struct Double_tE150EF3D1D43DEE85D533810AB4C742307EEDE5F 
{
	// System.Double System.Double::m_value
	double ___m_value_0;
};

// System.Int32
struct Int32_t680FF22E76F6EFAD4375103CBBFFA0421349384C 
{
	// System.Int32 System.Int32::m_value
	int32_t ___m_value_0;
};

// System.IntPtr
struct IntPtr_t 
{
	// System.Void* System.IntPtr::m_value
	void* ___m_value_0;
};

struct IntPtr_t_StaticFields
{
	// System.IntPtr System.IntPtr::Zero
	intptr_t ___Zero_1;
};

// UnityEngine.Mathf
struct Mathf_tE284D016E3B297B72311AAD9EB8F0E643F6A4682 
{
	union
	{
		struct
		{
		};
		uint8_t Mathf_tE284D016E3B297B72311AAD9EB8F0E643F6A4682__padding[1];
	};
};

struct Mathf_tE284D016E3B297B72311AAD9EB8F0E643F6A4682_StaticFields
{
	// System.Single UnityEngine.Mathf::Epsilon
	float ___Epsilon_0;
};

// UnityEngine.UI.Navigation
struct Navigation_t4D2E201D65749CF4E104E8AC1232CF1D6F14795C 
{
	// UnityEngine.UI.Navigation/Mode UnityEngine.UI.Navigation::m_Mode
	int32_t ___m_Mode_0;
	// System.Boolean UnityEngine.UI.Navigation::m_WrapAround
	bool ___m_WrapAround_1;
	// UnityEngine.UI.Selectable UnityEngine.UI.Navigation::m_SelectOnUp
	Selectable_t3251808068A17B8E92FB33590A4C2FA66D456712* ___m_SelectOnUp_2;
	// UnityEngine.UI.Selectable UnityEngine.UI.Navigation::m_SelectOnDown
	Selectable_t3251808068A17B8E92FB33590A4C2FA66D456712* ___m_SelectOnDown_3;
	// UnityEngine.UI.Selectable UnityEngine.UI.Navigation::m_SelectOnLeft
	Selectable_t3251808068A17B8E92FB33590A4C2FA66D456712* ___m_SelectOnLeft_4;
	// UnityEngine.UI.Selectable UnityEngine.UI.Navigation::m_SelectOnRight
	Selectable_t3251808068A17B8E92FB33590A4C2FA66D456712* ___m_SelectOnRight_5;
};
// Native definition for P/Invoke marshalling of UnityEngine.UI.Navigation
struct Navigation_t4D2E201D65749CF4E104E8AC1232CF1D6F14795C_marshaled_pinvoke
{
	int32_t ___m_Mode_0;
	int32_t ___m_WrapAround_1;
	Selectable_t3251808068A17B8E92FB33590A4C2FA66D456712* ___m_SelectOnUp_2;
	Selectable_t3251808068A17B8E92FB33590A4C2FA66D456712* ___m_SelectOnDown_3;
	Selectable_t3251808068A17B8E92FB33590A4C2FA66D456712* ___m_SelectOnLeft_4;
	Selectable_t3251808068A17B8E92FB33590A4C2FA66D456712* ___m_SelectOnRight_5;
};
// Native definition for COM marshalling of UnityEngine.UI.Navigation
struct Navigation_t4D2E201D65749CF4E104E8AC1232CF1D6F14795C_marshaled_com
{
	int32_t ___m_Mode_0;
	int32_t ___m_WrapAround_1;
	Selectable_t3251808068A17B8E92FB33590A4C2FA66D456712* ___m_SelectOnUp_2;
	Selectable_t3251808068A17B8E92FB33590A4C2FA66D456712* ___m_SelectOnDown_3;
	Selectable_t3251808068A17B8E92FB33590A4C2FA66D456712* ___m_SelectOnLeft_4;
	Selectable_t3251808068A17B8E92FB33590A4C2FA66D456712* ___m_SelectOnRight_5;
};

// UnityEngine.Quaternion
struct Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 
{
	// System.Single UnityEngine.Quaternion::x
	float ___x_0;
	// System.Single UnityEngine.Quaternion::y
	float ___y_1;
	// System.Single UnityEngine.Quaternion::z
	float ___z_2;
	// System.Single UnityEngine.Quaternion::w
	float ___w_3;
};

struct Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974_StaticFields
{
	// UnityEngine.Quaternion UnityEngine.Quaternion::identityQuaternion
	Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 ___identityQuaternion_4;
};

// System.Single
struct Single_t4530F2FF86FCB0DC29F35385CA1BD21BE294761C 
{
	// System.Single System.Single::m_value
	float ___m_value_0;
};

// UnityEngine.UI.SpriteState
struct SpriteState_tC8199570BE6337FB5C49347C97892B4222E5AACD 
{
	// UnityEngine.Sprite UnityEngine.UI.SpriteState::m_HighlightedSprite
	Sprite_tAFF74BC83CD68037494CB0B4F28CBDF8971CAB99* ___m_HighlightedSprite_0;
	// UnityEngine.Sprite UnityEngine.UI.SpriteState::m_PressedSprite
	Sprite_tAFF74BC83CD68037494CB0B4F28CBDF8971CAB99* ___m_PressedSprite_1;
	// UnityEngine.Sprite UnityEngine.UI.SpriteState::m_SelectedSprite
	Sprite_tAFF74BC83CD68037494CB0B4F28CBDF8971CAB99* ___m_SelectedSprite_2;
	// UnityEngine.Sprite UnityEngine.UI.SpriteState::m_DisabledSprite
	Sprite_tAFF74BC83CD68037494CB0B4F28CBDF8971CAB99* ___m_DisabledSprite_3;
};
// Native definition for P/Invoke marshalling of UnityEngine.UI.SpriteState
struct SpriteState_tC8199570BE6337FB5C49347C97892B4222E5AACD_marshaled_pinvoke
{
	Sprite_tAFF74BC83CD68037494CB0B4F28CBDF8971CAB99* ___m_HighlightedSprite_0;
	Sprite_tAFF74BC83CD68037494CB0B4F28CBDF8971CAB99* ___m_PressedSprite_1;
	Sprite_tAFF74BC83CD68037494CB0B4F28CBDF8971CAB99* ___m_SelectedSprite_2;
	Sprite_tAFF74BC83CD68037494CB0B4F28CBDF8971CAB99* ___m_DisabledSprite_3;
};
// Native definition for COM marshalling of UnityEngine.UI.SpriteState
struct SpriteState_tC8199570BE6337FB5C49347C97892B4222E5AACD_marshaled_com
{
	Sprite_tAFF74BC83CD68037494CB0B4F28CBDF8971CAB99* ___m_HighlightedSprite_0;
	Sprite_tAFF74BC83CD68037494CB0B4F28CBDF8971CAB99* ___m_PressedSprite_1;
	Sprite_tAFF74BC83CD68037494CB0B4F28CBDF8971CAB99* ___m_SelectedSprite_2;
	Sprite_tAFF74BC83CD68037494CB0B4F28CBDF8971CAB99* ___m_DisabledSprite_3;
};

// DG.Tweening.Tween
struct Tween_t8CB06EBC48A5B6F5065C490E4F4909C18CE7983C  : public ABSSequentiable_t05DF85FC63E3650D2D4CF6ABBA0F43263EB8CE89
{
	// System.Single DG.Tweening.Tween::timeScale
	float ___timeScale_4;
	// System.Boolean DG.Tweening.Tween::isBackwards
	bool ___isBackwards_5;
	// System.Boolean DG.Tweening.Tween::isInverted
	bool ___isInverted_6;
	// System.Object DG.Tweening.Tween::id
	RuntimeObject* ___id_7;
	// System.String DG.Tweening.Tween::stringId
	String_t* ___stringId_8;
	// System.Int32 DG.Tweening.Tween::intId
	int32_t ___intId_9;
	// System.Object DG.Tweening.Tween::target
	RuntimeObject* ___target_10;
	// DG.Tweening.UpdateType DG.Tweening.Tween::updateType
	int32_t ___updateType_11;
	// System.Boolean DG.Tweening.Tween::isIndependentUpdate
	bool ___isIndependentUpdate_12;
	// DG.Tweening.TweenCallback DG.Tweening.Tween::onPlay
	TweenCallback_t7C8B8A38E7B30905FF1B83C943256EF23617BB24* ___onPlay_13;
	// DG.Tweening.TweenCallback DG.Tweening.Tween::onPause
	TweenCallback_t7C8B8A38E7B30905FF1B83C943256EF23617BB24* ___onPause_14;
	// DG.Tweening.TweenCallback DG.Tweening.Tween::onRewind
	TweenCallback_t7C8B8A38E7B30905FF1B83C943256EF23617BB24* ___onRewind_15;
	// DG.Tweening.TweenCallback DG.Tweening.Tween::onUpdate
	TweenCallback_t7C8B8A38E7B30905FF1B83C943256EF23617BB24* ___onUpdate_16;
	// DG.Tweening.TweenCallback DG.Tweening.Tween::onStepComplete
	TweenCallback_t7C8B8A38E7B30905FF1B83C943256EF23617BB24* ___onStepComplete_17;
	// DG.Tweening.TweenCallback DG.Tweening.Tween::onComplete
	TweenCallback_t7C8B8A38E7B30905FF1B83C943256EF23617BB24* ___onComplete_18;
	// DG.Tweening.TweenCallback DG.Tweening.Tween::onKill
	TweenCallback_t7C8B8A38E7B30905FF1B83C943256EF23617BB24* ___onKill_19;
	// DG.Tweening.TweenCallback`1<System.Int32> DG.Tweening.Tween::onWaypointChange
	TweenCallback_1_tF0ADCA0C226C9C243ACB55E67D852E4BB53AEB67* ___onWaypointChange_20;
	// System.Boolean DG.Tweening.Tween::isFrom
	bool ___isFrom_21;
	// System.Boolean DG.Tweening.Tween::isBlendable
	bool ___isBlendable_22;
	// System.Boolean DG.Tweening.Tween::isRecyclable
	bool ___isRecyclable_23;
	// System.Boolean DG.Tweening.Tween::isSpeedBased
	bool ___isSpeedBased_24;
	// System.Boolean DG.Tweening.Tween::autoKill
	bool ___autoKill_25;
	// System.Single DG.Tweening.Tween::duration
	float ___duration_26;
	// System.Int32 DG.Tweening.Tween::loops
	int32_t ___loops_27;
	// DG.Tweening.LoopType DG.Tweening.Tween::loopType
	int32_t ___loopType_28;
	// System.Single DG.Tweening.Tween::delay
	float ___delay_29;
	// System.Boolean DG.Tweening.Tween::<isRelative>k__BackingField
	bool ___U3CisRelativeU3Ek__BackingField_30;
	// DG.Tweening.Ease DG.Tweening.Tween::easeType
	int32_t ___easeType_31;
	// DG.Tweening.EaseFunction DG.Tweening.Tween::customEase
	EaseFunction_t0F945D9D726B0915C5FBF30862E987EC3AC12A04* ___customEase_32;
	// System.Single DG.Tweening.Tween::easeOvershootOrAmplitude
	float ___easeOvershootOrAmplitude_33;
	// System.Single DG.Tweening.Tween::easePeriod
	float ___easePeriod_34;
	// System.String DG.Tweening.Tween::debugTargetId
	String_t* ___debugTargetId_35;
	// System.Type DG.Tweening.Tween::typeofT1
	Type_t* ___typeofT1_36;
	// System.Type DG.Tweening.Tween::typeofT2
	Type_t* ___typeofT2_37;
	// System.Type DG.Tweening.Tween::typeofTPlugOptions
	Type_t* ___typeofTPlugOptions_38;
	// System.Boolean DG.Tweening.Tween::<active>k__BackingField
	bool ___U3CactiveU3Ek__BackingField_39;
	// System.Boolean DG.Tweening.Tween::isSequenced
	bool ___isSequenced_40;
	// DG.Tweening.Sequence DG.Tweening.Tween::sequenceParent
	Sequence_tEADBE56D6ED2E9EE8FB2E5459C3E57131EC0545C* ___sequenceParent_41;
	// System.Int32 DG.Tweening.Tween::activeId
	int32_t ___activeId_42;
	// DG.Tweening.Core.Enums.SpecialStartupMode DG.Tweening.Tween::specialStartupMode
	int32_t ___specialStartupMode_43;
	// System.Boolean DG.Tweening.Tween::creationLocked
	bool ___creationLocked_44;
	// System.Boolean DG.Tweening.Tween::startupDone
	bool ___startupDone_45;
	// System.Boolean DG.Tweening.Tween::<playedOnce>k__BackingField
	bool ___U3CplayedOnceU3Ek__BackingField_46;
	// System.Single DG.Tweening.Tween::<position>k__BackingField
	float ___U3CpositionU3Ek__BackingField_47;
	// System.Single DG.Tweening.Tween::fullDuration
	float ___fullDuration_48;
	// System.Int32 DG.Tweening.Tween::completedLoops
	int32_t ___completedLoops_49;
	// System.Boolean DG.Tweening.Tween::isPlaying
	bool ___isPlaying_50;
	// System.Boolean DG.Tweening.Tween::isComplete
	bool ___isComplete_51;
	// System.Single DG.Tweening.Tween::elapsedDelay
	float ___elapsedDelay_52;
	// System.Boolean DG.Tweening.Tween::delayComplete
	bool ___delayComplete_53;
	// System.Int32 DG.Tweening.Tween::miscInt
	int32_t ___miscInt_54;
};

// UnityEngine.Events.UnityEvent
struct UnityEvent_tDC2C3548799DBC91D1E3F3DE60083A66F4751977  : public UnityEventBase_t4968A4C72559F35C0923E4BD9C042C3A842E1DB8
{
	// System.Object[] UnityEngine.Events.UnityEvent::m_InvokeArray
	ObjectU5BU5D_t8061030B0A12A55D5AD8652A20C922FE99450918* ___m_InvokeArray_3;
};

// UnityEngine.Vector2
struct Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 
{
	// System.Single UnityEngine.Vector2::x
	float ___x_0;
	// System.Single UnityEngine.Vector2::y
	float ___y_1;
};

struct Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7_StaticFields
{
	// UnityEngine.Vector2 UnityEngine.Vector2::zeroVector
	Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 ___zeroVector_2;
	// UnityEngine.Vector2 UnityEngine.Vector2::oneVector
	Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 ___oneVector_3;
	// UnityEngine.Vector2 UnityEngine.Vector2::upVector
	Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 ___upVector_4;
	// UnityEngine.Vector2 UnityEngine.Vector2::downVector
	Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 ___downVector_5;
	// UnityEngine.Vector2 UnityEngine.Vector2::leftVector
	Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 ___leftVector_6;
	// UnityEngine.Vector2 UnityEngine.Vector2::rightVector
	Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 ___rightVector_7;
	// UnityEngine.Vector2 UnityEngine.Vector2::positiveInfinityVector
	Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 ___positiveInfinityVector_8;
	// UnityEngine.Vector2 UnityEngine.Vector2::negativeInfinityVector
	Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 ___negativeInfinityVector_9;
};

// UnityEngine.Vector2Int
struct Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A 
{
	// System.Int32 UnityEngine.Vector2Int::m_X
	int32_t ___m_X_0;
	// System.Int32 UnityEngine.Vector2Int::m_Y
	int32_t ___m_Y_1;
};

struct Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A_StaticFields
{
	// UnityEngine.Vector2Int UnityEngine.Vector2Int::s_Zero
	Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A ___s_Zero_2;
	// UnityEngine.Vector2Int UnityEngine.Vector2Int::s_One
	Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A ___s_One_3;
	// UnityEngine.Vector2Int UnityEngine.Vector2Int::s_Up
	Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A ___s_Up_4;
	// UnityEngine.Vector2Int UnityEngine.Vector2Int::s_Down
	Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A ___s_Down_5;
	// UnityEngine.Vector2Int UnityEngine.Vector2Int::s_Left
	Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A ___s_Left_6;
	// UnityEngine.Vector2Int UnityEngine.Vector2Int::s_Right
	Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A ___s_Right_7;
};

// UnityEngine.Vector3
struct Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 
{
	// System.Single UnityEngine.Vector3::x
	float ___x_2;
	// System.Single UnityEngine.Vector3::y
	float ___y_3;
	// System.Single UnityEngine.Vector3::z
	float ___z_4;
};

struct Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2_StaticFields
{
	// UnityEngine.Vector3 UnityEngine.Vector3::zeroVector
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___zeroVector_5;
	// UnityEngine.Vector3 UnityEngine.Vector3::oneVector
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___oneVector_6;
	// UnityEngine.Vector3 UnityEngine.Vector3::upVector
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___upVector_7;
	// UnityEngine.Vector3 UnityEngine.Vector3::downVector
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___downVector_8;
	// UnityEngine.Vector3 UnityEngine.Vector3::leftVector
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___leftVector_9;
	// UnityEngine.Vector3 UnityEngine.Vector3::rightVector
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___rightVector_10;
	// UnityEngine.Vector3 UnityEngine.Vector3::forwardVector
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___forwardVector_11;
	// UnityEngine.Vector3 UnityEngine.Vector3::backVector
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___backVector_12;
	// UnityEngine.Vector3 UnityEngine.Vector3::positiveInfinityVector
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___positiveInfinityVector_13;
	// UnityEngine.Vector3 UnityEngine.Vector3::negativeInfinityVector
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___negativeInfinityVector_14;
};

// DG.Tweening.Plugins.Options.VectorOptions
struct VectorOptions_t2814CC842518C92C9DFC5DE6F7A73824758D3EF9 
{
	// DG.Tweening.AxisConstraint DG.Tweening.Plugins.Options.VectorOptions::axisConstraint
	int32_t ___axisConstraint_0;
	// System.Boolean DG.Tweening.Plugins.Options.VectorOptions::snapping
	bool ___snapping_1;
};
// Native definition for P/Invoke marshalling of DG.Tweening.Plugins.Options.VectorOptions
struct VectorOptions_t2814CC842518C92C9DFC5DE6F7A73824758D3EF9_marshaled_pinvoke
{
	int32_t ___axisConstraint_0;
	int32_t ___snapping_1;
};
// Native definition for COM marshalling of DG.Tweening.Plugins.Options.VectorOptions
struct VectorOptions_t2814CC842518C92C9DFC5DE6F7A73824758D3EF9_marshaled_com
{
	int32_t ___axisConstraint_0;
	int32_t ___snapping_1;
};

// System.Void
struct Void_t4861ACF8F4594C3437BB48B6E56783494B843915 
{
	union
	{
		struct
		{
		};
		uint8_t Void_t4861ACF8F4594C3437BB48B6E56783494B843915__padding[1];
	};
};

// <PrivateImplementationDetails>/__StaticArrayInitTypeSize=24
struct __StaticArrayInitTypeSizeU3D24_t3464DA68B6CCAB9A0A43F94B3DB9AA7E7FDDB19A 
{
	union
	{
		struct
		{
			union
			{
			};
		};
		uint8_t __StaticArrayInitTypeSizeU3D24_t3464DA68B6CCAB9A0A43F94B3DB9AA7E7FDDB19A__padding[24];
	};
};

// System.Collections.Generic.List`1/Enumerator<UnityEngine.Vector2Int>
struct Enumerator_t4BD1B5394A2E9960562CC364C2CCE4FCBD1AC258 
{
	// System.Collections.Generic.List`1<T> System.Collections.Generic.List`1/Enumerator::_list
	List_1_tB56F1028A724D2CE4E84861619D1CF68C68C983D* ____list_0;
	// System.Int32 System.Collections.Generic.List`1/Enumerator::_index
	int32_t ____index_1;
	// System.Int32 System.Collections.Generic.List`1/Enumerator::_version
	int32_t ____version_2;
	// T System.Collections.Generic.List`1/Enumerator::_current
	Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A ____current_3;
};

// System.Nullable`1<UnityEngine.Color>
struct Nullable_1_tEE83D90B507D40B6C58B5EEF5B9D44D377B44F11 
{
	// System.Boolean System.Nullable`1::hasValue
	bool ___hasValue_0;
	// T System.Nullable`1::value
	Color_tD001788D726C3A7F1379BEED0260B9591F440C1F ___value_1;
};

// <PrivateImplementationDetails>
struct U3CPrivateImplementationDetailsU3E_t0F5473E849A5A5185A9F4C5246F0C32816C49FCA  : public RuntimeObject
{
};

struct U3CPrivateImplementationDetailsU3E_t0F5473E849A5A5185A9F4C5246F0C32816C49FCA_StaticFields
{
	// <PrivateImplementationDetails>/__StaticArrayInitTypeSize=24 <PrivateImplementationDetails>::CD9A54ED1F18BF97DB08914E280EA7349E11CA2C4885A4D8052552CEBA84208D
	__StaticArrayInitTypeSizeU3D24_t3464DA68B6CCAB9A0A43F94B3DB9AA7E7FDDB19A ___CD9A54ED1F18BF97DB08914E280EA7349E11CA2C4885A4D8052552CEBA84208D_0;
};

// UnityEngine.UI.ColorBlock
struct ColorBlock_tDD7C62E7AFE442652FC98F8D058CE8AE6BFD7C11 
{
	// UnityEngine.Color UnityEngine.UI.ColorBlock::m_NormalColor
	Color_tD001788D726C3A7F1379BEED0260B9591F440C1F ___m_NormalColor_0;
	// UnityEngine.Color UnityEngine.UI.ColorBlock::m_HighlightedColor
	Color_tD001788D726C3A7F1379BEED0260B9591F440C1F ___m_HighlightedColor_1;
	// UnityEngine.Color UnityEngine.UI.ColorBlock::m_PressedColor
	Color_tD001788D726C3A7F1379BEED0260B9591F440C1F ___m_PressedColor_2;
	// UnityEngine.Color UnityEngine.UI.ColorBlock::m_SelectedColor
	Color_tD001788D726C3A7F1379BEED0260B9591F440C1F ___m_SelectedColor_3;
	// UnityEngine.Color UnityEngine.UI.ColorBlock::m_DisabledColor
	Color_tD001788D726C3A7F1379BEED0260B9591F440C1F ___m_DisabledColor_4;
	// System.Single UnityEngine.UI.ColorBlock::m_ColorMultiplier
	float ___m_ColorMultiplier_5;
	// System.Single UnityEngine.UI.ColorBlock::m_FadeDuration
	float ___m_FadeDuration_6;
};

struct ColorBlock_tDD7C62E7AFE442652FC98F8D058CE8AE6BFD7C11_StaticFields
{
	// UnityEngine.UI.ColorBlock UnityEngine.UI.ColorBlock::defaultColorBlock
	ColorBlock_tDD7C62E7AFE442652FC98F8D058CE8AE6BFD7C11 ___defaultColorBlock_7;
};

// UnityEngine.Coroutine
struct Coroutine_t85EA685566A254C23F3FD77AB5BDFFFF8799596B  : public YieldInstruction_tFCE35FD0907950EFEE9BC2890AC664E41C53728D
{
	// System.IntPtr UnityEngine.Coroutine::m_Ptr
	intptr_t ___m_Ptr_0;
};
// Native definition for P/Invoke marshalling of UnityEngine.Coroutine
struct Coroutine_t85EA685566A254C23F3FD77AB5BDFFFF8799596B_marshaled_pinvoke : public YieldInstruction_tFCE35FD0907950EFEE9BC2890AC664E41C53728D_marshaled_pinvoke
{
	intptr_t ___m_Ptr_0;
};
// Native definition for COM marshalling of UnityEngine.Coroutine
struct Coroutine_t85EA685566A254C23F3FD77AB5BDFFFF8799596B_marshaled_com : public YieldInstruction_tFCE35FD0907950EFEE9BC2890AC664E41C53728D_marshaled_com
{
	intptr_t ___m_Ptr_0;
};

// System.Delegate
struct Delegate_t  : public RuntimeObject
{
	// System.IntPtr System.Delegate::method_ptr
	Il2CppMethodPointer ___method_ptr_0;
	// System.IntPtr System.Delegate::invoke_impl
	intptr_t ___invoke_impl_1;
	// System.Object System.Delegate::m_target
	RuntimeObject* ___m_target_2;
	// System.IntPtr System.Delegate::method
	intptr_t ___method_3;
	// System.IntPtr System.Delegate::delegate_trampoline
	intptr_t ___delegate_trampoline_4;
	// System.IntPtr System.Delegate::extra_arg
	intptr_t ___extra_arg_5;
	// System.IntPtr System.Delegate::method_code
	intptr_t ___method_code_6;
	// System.IntPtr System.Delegate::interp_method
	intptr_t ___interp_method_7;
	// System.IntPtr System.Delegate::interp_invoke_impl
	intptr_t ___interp_invoke_impl_8;
	// System.Reflection.MethodInfo System.Delegate::method_info
	MethodInfo_t* ___method_info_9;
	// System.Reflection.MethodInfo System.Delegate::original_method_info
	MethodInfo_t* ___original_method_info_10;
	// System.DelegateData System.Delegate::data
	DelegateData_t9B286B493293CD2D23A5B2B5EF0E5B1324C2B77E* ___data_11;
	// System.Boolean System.Delegate::method_is_virtual
	bool ___method_is_virtual_12;
};
// Native definition for P/Invoke marshalling of System.Delegate
struct Delegate_t_marshaled_pinvoke
{
	intptr_t ___method_ptr_0;
	intptr_t ___invoke_impl_1;
	Il2CppIUnknown* ___m_target_2;
	intptr_t ___method_3;
	intptr_t ___delegate_trampoline_4;
	intptr_t ___extra_arg_5;
	intptr_t ___method_code_6;
	intptr_t ___interp_method_7;
	intptr_t ___interp_invoke_impl_8;
	MethodInfo_t* ___method_info_9;
	MethodInfo_t* ___original_method_info_10;
	DelegateData_t9B286B493293CD2D23A5B2B5EF0E5B1324C2B77E* ___data_11;
	int32_t ___method_is_virtual_12;
};
// Native definition for COM marshalling of System.Delegate
struct Delegate_t_marshaled_com
{
	intptr_t ___method_ptr_0;
	intptr_t ___invoke_impl_1;
	Il2CppIUnknown* ___m_target_2;
	intptr_t ___method_3;
	intptr_t ___delegate_trampoline_4;
	intptr_t ___extra_arg_5;
	intptr_t ___method_code_6;
	intptr_t ___interp_method_7;
	intptr_t ___interp_invoke_impl_8;
	MethodInfo_t* ___method_info_9;
	MethodInfo_t* ___original_method_info_10;
	DelegateData_t9B286B493293CD2D23A5B2B5EF0E5B1324C2B77E* ___data_11;
	int32_t ___method_is_virtual_12;
};

// System.Exception
struct Exception_t  : public RuntimeObject
{
	// System.String System.Exception::_className
	String_t* ____className_1;
	// System.String System.Exception::_message
	String_t* ____message_2;
	// System.Collections.IDictionary System.Exception::_data
	RuntimeObject* ____data_3;
	// System.Exception System.Exception::_innerException
	Exception_t* ____innerException_4;
	// System.String System.Exception::_helpURL
	String_t* ____helpURL_5;
	// System.Object System.Exception::_stackTrace
	RuntimeObject* ____stackTrace_6;
	// System.String System.Exception::_stackTraceString
	String_t* ____stackTraceString_7;
	// System.String System.Exception::_remoteStackTraceString
	String_t* ____remoteStackTraceString_8;
	// System.Int32 System.Exception::_remoteStackIndex
	int32_t ____remoteStackIndex_9;
	// System.Object System.Exception::_dynamicMethods
	RuntimeObject* ____dynamicMethods_10;
	// System.Int32 System.Exception::_HResult
	int32_t ____HResult_11;
	// System.String System.Exception::_source
	String_t* ____source_12;
	// System.Runtime.Serialization.SafeSerializationManager System.Exception::_safeSerializationManager
	SafeSerializationManager_tCBB85B95DFD1634237140CD892E82D06ECB3F5E6* ____safeSerializationManager_13;
	// System.Diagnostics.StackTrace[] System.Exception::captured_traces
	StackTraceU5BU5D_t32FBCB20930EAF5BAE3F450FF75228E5450DA0DF* ___captured_traces_14;
	// System.IntPtr[] System.Exception::native_trace_ips
	IntPtrU5BU5D_tFD177F8C806A6921AD7150264CCC62FA00CAD832* ___native_trace_ips_15;
	// System.Int32 System.Exception::caught_in_unmanaged
	int32_t ___caught_in_unmanaged_16;
};

struct Exception_t_StaticFields
{
	// System.Object System.Exception::s_EDILock
	RuntimeObject* ___s_EDILock_0;
};
// Native definition for P/Invoke marshalling of System.Exception
struct Exception_t_marshaled_pinvoke
{
	char* ____className_1;
	char* ____message_2;
	RuntimeObject* ____data_3;
	Exception_t_marshaled_pinvoke* ____innerException_4;
	char* ____helpURL_5;
	Il2CppIUnknown* ____stackTrace_6;
	char* ____stackTraceString_7;
	char* ____remoteStackTraceString_8;
	int32_t ____remoteStackIndex_9;
	Il2CppIUnknown* ____dynamicMethods_10;
	int32_t ____HResult_11;
	char* ____source_12;
	SafeSerializationManager_tCBB85B95DFD1634237140CD892E82D06ECB3F5E6* ____safeSerializationManager_13;
	StackTraceU5BU5D_t32FBCB20930EAF5BAE3F450FF75228E5450DA0DF* ___captured_traces_14;
	Il2CppSafeArray/*NONE*/* ___native_trace_ips_15;
	int32_t ___caught_in_unmanaged_16;
};
// Native definition for COM marshalling of System.Exception
struct Exception_t_marshaled_com
{
	Il2CppChar* ____className_1;
	Il2CppChar* ____message_2;
	RuntimeObject* ____data_3;
	Exception_t_marshaled_com* ____innerException_4;
	Il2CppChar* ____helpURL_5;
	Il2CppIUnknown* ____stackTrace_6;
	Il2CppChar* ____stackTraceString_7;
	Il2CppChar* ____remoteStackTraceString_8;
	int32_t ____remoteStackIndex_9;
	Il2CppIUnknown* ____dynamicMethods_10;
	int32_t ____HResult_11;
	Il2CppChar* ____source_12;
	SafeSerializationManager_tCBB85B95DFD1634237140CD892E82D06ECB3F5E6* ____safeSerializationManager_13;
	StackTraceU5BU5D_t32FBCB20930EAF5BAE3F450FF75228E5450DA0DF* ___captured_traces_14;
	Il2CppSafeArray/*NONE*/* ___native_trace_ips_15;
	int32_t ___caught_in_unmanaged_16;
};

// UnityEngine.Object
struct Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C  : public RuntimeObject
{
	// System.IntPtr UnityEngine.Object::m_CachedPtr
	intptr_t ___m_CachedPtr_0;
};

struct Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_StaticFields
{
	// System.Int32 UnityEngine.Object::OffsetOfInstanceIDInCPlusPlusObject
	int32_t ___OffsetOfInstanceIDInCPlusPlusObject_1;
};
// Native definition for P/Invoke marshalling of UnityEngine.Object
struct Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_marshaled_pinvoke
{
	intptr_t ___m_CachedPtr_0;
};
// Native definition for COM marshalling of UnityEngine.Object
struct Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_marshaled_com
{
	intptr_t ___m_CachedPtr_0;
};

// DG.Tweening.Plugins.Options.QuaternionOptions
struct QuaternionOptions_t1B83700718F7417854E4B6FB0E1726E183F69718 
{
	// DG.Tweening.RotateMode DG.Tweening.Plugins.Options.QuaternionOptions::rotateMode
	int32_t ___rotateMode_0;
	// DG.Tweening.AxisConstraint DG.Tweening.Plugins.Options.QuaternionOptions::axisConstraint
	int32_t ___axisConstraint_1;
	// UnityEngine.Vector3 DG.Tweening.Plugins.Options.QuaternionOptions::up
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___up_2;
	// System.Boolean DG.Tweening.Plugins.Options.QuaternionOptions::dynamicLookAt
	bool ___dynamicLookAt_3;
	// UnityEngine.Vector3 DG.Tweening.Plugins.Options.QuaternionOptions::dynamicLookAtWorldPosition
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___dynamicLookAtWorldPosition_4;
};
// Native definition for P/Invoke marshalling of DG.Tweening.Plugins.Options.QuaternionOptions
struct QuaternionOptions_t1B83700718F7417854E4B6FB0E1726E183F69718_marshaled_pinvoke
{
	int32_t ___rotateMode_0;
	int32_t ___axisConstraint_1;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___up_2;
	int32_t ___dynamicLookAt_3;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___dynamicLookAtWorldPosition_4;
};
// Native definition for COM marshalling of DG.Tweening.Plugins.Options.QuaternionOptions
struct QuaternionOptions_t1B83700718F7417854E4B6FB0E1726E183F69718_marshaled_com
{
	int32_t ___rotateMode_0;
	int32_t ___axisConstraint_1;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___up_2;
	int32_t ___dynamicLookAt_3;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___dynamicLookAtWorldPosition_4;
};

// System.RuntimeFieldHandle
struct RuntimeFieldHandle_t6E4C45B6D2EA12FC99185805A7E77527899B25C5 
{
	// System.IntPtr System.RuntimeFieldHandle::value
	intptr_t ___value_0;
};

// DG.Tweening.Tweener
struct Tweener_tD38633F1A42EDF47A73CE3BF1894D946E830E140  : public Tween_t8CB06EBC48A5B6F5065C490E4F4909C18CE7983C
{
	// System.Boolean DG.Tweening.Tweener::hasManuallySetStartValue
	bool ___hasManuallySetStartValue_55;
	// System.Boolean DG.Tweening.Tweener::isFromAllowed
	bool ___isFromAllowed_56;
};

// UnityEngine.UI.Button/ButtonClickedEvent
struct ButtonClickedEvent_t8EA72E90B3BD1392FB3B3EF167D5121C23569E4C  : public UnityEvent_tDC2C3548799DBC91D1E3F3DE60083A66F4751977
{
};

// CrowdMatch.GameController/<MoveToGatherPoint>d__45
struct U3CMoveToGatherPointU3Ed__45_t540C804CD76258EA246336B338FC828D346240AF  : public RuntimeObject
{
	// System.Int32 CrowdMatch.GameController/<MoveToGatherPoint>d__45::<>1__state
	int32_t ___U3CU3E1__state_0;
	// System.Object CrowdMatch.GameController/<MoveToGatherPoint>d__45::<>2__current
	RuntimeObject* ___U3CU3E2__current_1;
	// CrowdMatch.PixelItem CrowdMatch.GameController/<MoveToGatherPoint>d__45::item
	PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* ___item_2;
	// CrowdMatch.GameController CrowdMatch.GameController/<MoveToGatherPoint>d__45::<>4__this
	GameController_t9B394943D9DA551993B8515B21F692D9B0E00853* ___U3CU3E4__this_3;
	// UnityEngine.Vector3 CrowdMatch.GameController/<MoveToGatherPoint>d__45::<start>5__2
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___U3CstartU3E5__2_4;
	// UnityEngine.Vector3 CrowdMatch.GameController/<MoveToGatherPoint>d__45::<target>5__3
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___U3CtargetU3E5__3_5;
	// System.Single CrowdMatch.GameController/<MoveToGatherPoint>d__45::<duration>5__4
	float ___U3CdurationU3E5__4_6;
	// System.Single CrowdMatch.GameController/<MoveToGatherPoint>d__45::<t>5__5
	float ___U3CtU3E5__5_7;
};

// CrowdMatch.PixelItem/<MoveExposeTargetToY>d__35
struct U3CMoveExposeTargetToYU3Ed__35_tAD80531ED95A191AB90EE89508DFC17246399033  : public RuntimeObject
{
	// System.Int32 CrowdMatch.PixelItem/<MoveExposeTargetToY>d__35::<>1__state
	int32_t ___U3CU3E1__state_0;
	// System.Object CrowdMatch.PixelItem/<MoveExposeTargetToY>d__35::<>2__current
	RuntimeObject* ___U3CU3E2__current_1;
	// CrowdMatch.PixelItem CrowdMatch.PixelItem/<MoveExposeTargetToY>d__35::<>4__this
	PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* ___U3CU3E4__this_2;
	// System.Single CrowdMatch.PixelItem/<MoveExposeTargetToY>d__35::targetY
	float ___targetY_3;
	// System.Single CrowdMatch.PixelItem/<MoveExposeTargetToY>d__35::duration
	float ___duration_4;
	// UnityEngine.Transform CrowdMatch.PixelItem/<MoveExposeTargetToY>d__35::<t>5__2
	Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* ___U3CtU3E5__2_5;
	// UnityEngine.Vector3 CrowdMatch.PixelItem/<MoveExposeTargetToY>d__35::<start>5__3
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___U3CstartU3E5__3_6;
	// UnityEngine.Vector3 CrowdMatch.PixelItem/<MoveExposeTargetToY>d__35::<target>5__4
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___U3CtargetU3E5__4_7;
	// System.Single CrowdMatch.PixelItem/<MoveExposeTargetToY>d__35::<dur>5__5
	float ___U3CdurU3E5__5_8;
	// System.Single CrowdMatch.PixelItem/<MoveExposeTargetToY>d__35::<elapsed>5__6
	float ___U3CelapsedU3E5__6_9;
};

// DG.Tweening.Core.TweenerCore`3<UnityEngine.Quaternion,UnityEngine.Vector3,DG.Tweening.Plugins.Options.QuaternionOptions>
struct TweenerCore_3_t392C54729BB024F5802F8E205C583653C4E886E3  : public Tweener_tD38633F1A42EDF47A73CE3BF1894D946E830E140
{
	// T2 DG.Tweening.Core.TweenerCore`3::startValue
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___startValue_57;
	// T2 DG.Tweening.Core.TweenerCore`3::endValue
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___endValue_58;
	// T2 DG.Tweening.Core.TweenerCore`3::changeValue
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___changeValue_59;
	// TPlugOptions DG.Tweening.Core.TweenerCore`3::plugOptions
	QuaternionOptions_t1B83700718F7417854E4B6FB0E1726E183F69718 ___plugOptions_60;
	// DG.Tweening.Core.DOGetter`1<T1> DG.Tweening.Core.TweenerCore`3::getter
	DOGetter_1_tB89DD12456B8E79576BB70E1CA6DF899686410D3* ___getter_61;
	// DG.Tweening.Core.DOSetter`1<T1> DG.Tweening.Core.TweenerCore`3::setter
	DOSetter_1_t9EFF8DD70A15F455A6FE698A22BD0FE9683AC28E* ___setter_62;
	// DG.Tweening.Plugins.Core.ABSTweenPlugin`3<T1,T2,TPlugOptions> DG.Tweening.Core.TweenerCore`3::tweenPlugin
	ABSTweenPlugin_3_t3823C0F935A3168B9E48DC90ABD9A0CED3D7BB82* ___tweenPlugin_63;
	// System.Type DG.Tweening.Core.TweenerCore`3::_colorType
	Type_t* ____colorType_65;
	// System.Type DG.Tweening.Core.TweenerCore`3::_color32Type
	Type_t* ____color32Type_66;
};

// DG.Tweening.Core.TweenerCore`3<UnityEngine.Vector3,UnityEngine.Vector3,DG.Tweening.Plugins.Options.VectorOptions>
struct TweenerCore_3_tCD82DFC45FB71C681FA8659EA63A7D7D16BFFE77  : public Tweener_tD38633F1A42EDF47A73CE3BF1894D946E830E140
{
	// T2 DG.Tweening.Core.TweenerCore`3::startValue
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___startValue_57;
	// T2 DG.Tweening.Core.TweenerCore`3::endValue
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___endValue_58;
	// T2 DG.Tweening.Core.TweenerCore`3::changeValue
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___changeValue_59;
	// TPlugOptions DG.Tweening.Core.TweenerCore`3::plugOptions
	VectorOptions_t2814CC842518C92C9DFC5DE6F7A73824758D3EF9 ___plugOptions_60;
	// DG.Tweening.Core.DOGetter`1<T1> DG.Tweening.Core.TweenerCore`3::getter
	DOGetter_1_t709462C08281F3AA5DFEF36CAF91404B1004C338* ___getter_61;
	// DG.Tweening.Core.DOSetter`1<T1> DG.Tweening.Core.TweenerCore`3::setter
	DOSetter_1_t02E8F9920F174322F1CF5AC8BCDEAABD14A03358* ___setter_62;
	// DG.Tweening.Plugins.Core.ABSTweenPlugin`3<T1,T2,TPlugOptions> DG.Tweening.Core.TweenerCore`3::tweenPlugin
	ABSTweenPlugin_3_tE5A78BE46D046C07A6356B8AB596B2D00F9295E7* ___tweenPlugin_63;
	// System.Type DG.Tweening.Core.TweenerCore`3::_colorType
	Type_t* ____colorType_65;
	// System.Type DG.Tweening.Core.TweenerCore`3::_color32Type
	Type_t* ____color32Type_66;
};

// UnityEngine.Component
struct Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3  : public Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C
{
};

// UnityEngine.GameObject
struct GameObject_t76FEDD663AB33C991A9C9A23129337651094216F  : public Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C
{
};

// UnityEngine.Material
struct Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3  : public Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C
{
};

// System.MulticastDelegate
struct MulticastDelegate_t  : public Delegate_t
{
	// System.Delegate[] System.MulticastDelegate::delegates
	DelegateU5BU5D_tC5AB7E8F745616680F337909D3A8E6C722CDF771* ___delegates_13;
};
// Native definition for P/Invoke marshalling of System.MulticastDelegate
struct MulticastDelegate_t_marshaled_pinvoke : public Delegate_t_marshaled_pinvoke
{
	Delegate_t_marshaled_pinvoke** ___delegates_13;
};
// Native definition for COM marshalling of System.MulticastDelegate
struct MulticastDelegate_t_marshaled_com : public Delegate_t_marshaled_com
{
	Delegate_t_marshaled_com** ___delegates_13;
};

// UnityEngine.ScriptableObject
struct ScriptableObject_tB3BFDB921A1B1795B38A5417D3B97A89A140436A  : public Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C
{
};
// Native definition for P/Invoke marshalling of UnityEngine.ScriptableObject
struct ScriptableObject_tB3BFDB921A1B1795B38A5417D3B97A89A140436A_marshaled_pinvoke : public Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_marshaled_pinvoke
{
};
// Native definition for COM marshalling of UnityEngine.ScriptableObject
struct ScriptableObject_tB3BFDB921A1B1795B38A5417D3B97A89A140436A_marshaled_com : public Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_marshaled_com
{
};

// System.SystemException
struct SystemException_tCC48D868298F4C0705279823E34B00F4FBDB7295  : public Exception_t
{
};

// UnityEngine.Texture
struct Texture_t791CBB51219779964E0E8A2ED7C1AA5F92A4A700  : public Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C
{
};

struct Texture_t791CBB51219779964E0E8A2ED7C1AA5F92A4A700_StaticFields
{
	// System.Int32 UnityEngine.Texture::GenerateAllMips
	int32_t ___GenerateAllMips_4;
};

// System.Func`3<System.Int32,System.Int32,System.Nullable`1<UnityEngine.Color>>
struct Func_3_t171A20B65D1078C91C9A875EF6CBB2716C7E322D  : public MulticastDelegate_t
{
};

// System.Func`3<System.Int32,System.Int32,System.Boolean>
struct Func_3_tE8F85DA3CAC4998201E5C56356280AFAB7185B69  : public MulticastDelegate_t
{
};

// UnityEngine.Behaviour
struct Behaviour_t01970CFBBA658497AE30F311C447DB0440BAB7FA  : public Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3
{
};

// UnityEngine.Collider
struct Collider_t1CC3163924FCD6C4CC2E816373A929C1E3D55E76  : public Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3
{
};

// CrowdMatch.ColorConfig
struct ColorConfig_tCFE08A98D99EE62711B453124E88292D1545172C  : public ScriptableObject_tB3BFDB921A1B1795B38A5417D3B97A89A140436A
{
	// UnityEngine.Material[] CrowdMatch.ColorConfig::materials
	MaterialU5BU5D_t2B1D11C42DB07A4400C0535F92DBB87A2E346D3D* ___materials_4;
	// UnityEngine.Material[] CrowdMatch.ColorConfig::carMaterials
	MaterialU5BU5D_t2B1D11C42DB07A4400C0535F92DBB87A2E346D3D* ___carMaterials_5;
	// UnityEngine.Material[] CrowdMatch.ColorConfig::interiorMaterials
	MaterialU5BU5D_t2B1D11C42DB07A4400C0535F92DBB87A2E346D3D* ___interiorMaterials_6;
};

// System.NotSupportedException
struct NotSupportedException_t1429765983D409BD2986508963C98D214E4EBF4A  : public SystemException_tCC48D868298F4C0705279823E34B00F4FBDB7295
{
};

// UnityEngine.Renderer
struct Renderer_t320575F223BCB177A982E5DDB5DB19FAA89E7FBF  : public Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3
{
};

// UnityEngine.Texture2D
struct Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4  : public Texture_t791CBB51219779964E0E8A2ED7C1AA5F92A4A700
{
};

// UnityEngine.Transform
struct Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1  : public Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3
{
};

// DG.Tweening.TweenCallback
struct TweenCallback_t7C8B8A38E7B30905FF1B83C943256EF23617BB24  : public MulticastDelegate_t
{
};

// UnityEngine.Events.UnityAction
struct UnityAction_t11A1F3B953B365C072A5DCC32677EE1796A962A7  : public MulticastDelegate_t
{
};

// UnityEngine.Animator
struct Animator_t8A52E42AE54F76681838FE9E632683EF3952E883  : public Behaviour_t01970CFBBA658497AE30F311C447DB0440BAB7FA
{
};

// UnityEngine.BoxCollider
struct BoxCollider_tFA5D239388334D6DE0B8FFDAD6825C5B03786E23  : public Collider_t1CC3163924FCD6C4CC2E816373A929C1E3D55E76
{
};

// UnityEngine.MonoBehaviour
struct MonoBehaviour_t532A11E69716D348D8AA7F854AFCBFCB8AD17F71  : public Behaviour_t01970CFBBA658497AE30F311C447DB0440BAB7FA
{
};

// CrowdMatch.GameController
struct GameController_t9B394943D9DA551993B8515B21F692D9B0E00853  : public MonoBehaviour_t532A11E69716D348D8AA7F854AFCBFCB8AD17F71
{
	// UnityEngine.Transform CrowdMatch.GameController::gatherPoint
	Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* ___gatherPoint_5;
	// UnityEngine.UI.Text CrowdMatch.GameController::gatherCountText
	Text_tD60B2346DAA6666BF0D822FF607F0B220C2B9E62* ___gatherCountText_6;
	// CrowdMatch.PixelGroup CrowdMatch.GameController::pixelGroup
	PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* ___pixelGroup_7;
	// CrowdMatch.ContainerGroup CrowdMatch.GameController::containerGroup
	ContainerGroup_t4E8C2C211C9490625D25CE060D3AD94505CA9300* ___containerGroup_8;
	// System.Single CrowdMatch.GameController::gatherSpeed
	float ___gatherSpeed_9;
	// System.Single CrowdMatch.GameController::gatherScatterRadius
	float ___gatherScatterRadius_10;
	// CrowdMatch.CrowdBufferZone CrowdMatch.GameController::crowdBuffer
	CrowdBufferZone_t19D1AFC53A6064BCB80182E56B0DAFAA95723CBD* ___crowdBuffer_11;
	// CrowdMatch.ConveyorBeltZone CrowdMatch.GameController::conveyorZone
	ConveyorBeltZone_t02C9C7082253896FBB1D33FDCF4897E1865FA3DB* ___conveyorZone_12;
	// System.Boolean CrowdMatch.GameController::recordMode
	bool ___recordMode_13;
	// System.String CrowdMatch.GameController::recordOutputDir
	String_t* ___recordOutputDir_14;
	// System.Collections.Generic.List`1<CrowdMatch.PixelItem> CrowdMatch.GameController::gatheredItems
	List_1_tB8E48F8C3B88C3DD79D5A515A2A033D8CD5FEAD6* ___gatheredItems_15;
	// System.IO.StreamWriter CrowdMatch.GameController::_recordWriter
	StreamWriter_t6E7DF7D524AA3C018A65F62EE80779873ED4D1E4* ____recordWriter_16;
	// System.String CrowdMatch.GameController::_recordFilePath
	String_t* ____recordFilePath_17;
	// System.Boolean CrowdMatch.GameController::_transitioning
	bool ____transitioning_18;
	// System.Int32 CrowdMatch.GameController::_clickMask
	int32_t ____clickMask_19;
};

struct GameController_t9B394943D9DA551993B8515B21F692D9B0E00853_StaticFields
{
	// CrowdMatch.GameController CrowdMatch.GameController::<Instance>k__BackingField
	GameController_t9B394943D9DA551993B8515B21F692D9B0E00853* ___U3CInstanceU3Ek__BackingField_4;
};

// CrowdMatch.GameManager
struct GameManager_t5B23045F478AD8B0E3D943ECC8493384B02FF32F  : public MonoBehaviour_t532A11E69716D348D8AA7F854AFCBFCB8AD17F71
{
	// CrowdMatch.ColorConfig CrowdMatch.GameManager::colorConfig
	ColorConfig_tCFE08A98D99EE62711B453124E88292D1545172C* ___colorConfig_5;
	// System.Collections.Generic.List`1<UnityEngine.TextAsset> CrowdMatch.GameManager::levelJsons
	List_1_tC0FCC010411366A6623886AFA93A0B022E62D015* ___levelJsons_6;
	// CrowdMatch.LevelDataConfig CrowdMatch.GameManager::levelDataConfig
	LevelDataConfig_t640D26E9446ECE825FF36A677A524E81EC855513* ___levelDataConfig_7;
};

struct GameManager_t5B23045F478AD8B0E3D943ECC8493384B02FF32F_StaticFields
{
	// CrowdMatch.GameManager CrowdMatch.GameManager::<Instance>k__BackingField
	GameManager_t5B23045F478AD8B0E3D943ECC8493384B02FF32F* ___U3CInstanceU3Ek__BackingField_4;
};

// CrowdMatch.LevelSkipButtons
struct LevelSkipButtons_t116E0448EB9F81B180CA7AA70DCB08A06728621D  : public MonoBehaviour_t532A11E69716D348D8AA7F854AFCBFCB8AD17F71
{
	// UnityEngine.UI.Button CrowdMatch.LevelSkipButtons::nextButton
	Button_t6786514A57F7AFDEE5431112FEA0CAB24F5AE098* ___nextButton_4;
	// UnityEngine.UI.Button CrowdMatch.LevelSkipButtons::prevButton
	Button_t6786514A57F7AFDEE5431112FEA0CAB24F5AE098* ___prevButton_5;
};

// CrowdMatch.PixelClickListener
struct PixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524  : public MonoBehaviour_t532A11E69716D348D8AA7F854AFCBFCB8AD17F71
{
	// CrowdMatch.PixelItem CrowdMatch.PixelClickListener::pixel
	PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* ___pixel_4;
	// UnityEngine.BoxCollider CrowdMatch.PixelClickListener::<BoxCollider>k__BackingField
	BoxCollider_tFA5D239388334D6DE0B8FFDAD6825C5B03786E23* ___U3CBoxColliderU3Ek__BackingField_5;
};

// CrowdMatch.PixelGroup
struct PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80  : public MonoBehaviour_t532A11E69716D348D8AA7F854AFCBFCB8AD17F71
{
	// System.Single CrowdMatch.PixelGroup::unitSize
	float ___unitSize_4;
	// System.Single CrowdMatch.PixelGroup::spacingX
	float ___spacingX_5;
	// System.Single CrowdMatch.PixelGroup::spacingZ
	float ___spacingZ_6;
	// System.Int32 CrowdMatch.PixelGroup::columns
	int32_t ___columns_7;
	// System.Int32 CrowdMatch.PixelGroup::rows
	int32_t ___rows_8;
	// System.Int32 CrowdMatch.PixelGroup::tailRows
	int32_t ___tailRows_9;
	// System.Int32[] CrowdMatch.PixelGroup::colorIds
	Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* ___colorIds_10;
	// System.Int32 CrowdMatch.PixelGroup::minRunLength
	int32_t ___minRunLength_11;
	// System.Int32 CrowdMatch.PixelGroup::maxRunLength
	int32_t ___maxRunLength_12;
	// System.Boolean CrowdMatch.PixelGroup::fillToMultipleOf3
	bool ___fillToMultipleOf3_13;
	// UnityEngine.GameObject CrowdMatch.PixelGroup::pixelPrefab
	GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* ___pixelPrefab_14;
	// UnityEngine.GameObject CrowdMatch.PixelGroup::wallPrefab
	GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* ___wallPrefab_15;
	// CrowdMatch.PixelItem[,] CrowdMatch.PixelGroup::grid
	PixelItemU5BU2CU5D_t26ACEBF9031979698453CA52C61A20C0A1A68C7F* ___grid_16;
	// System.Boolean[,] CrowdMatch.PixelGroup::wallGrid
	BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* ___wallGrid_17;
};

// CrowdMatch.PixelItem
struct PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E  : public MonoBehaviour_t532A11E69716D348D8AA7F854AFCBFCB8AD17F71
{
	// System.Int32 CrowdMatch.PixelItem::colorId
	int32_t ___colorId_4;
	// System.Int32 CrowdMatch.PixelItem::gridX
	int32_t ___gridX_5;
	// System.Int32 CrowdMatch.PixelItem::gridZ
	int32_t ___gridZ_6;
	// System.Collections.Generic.List`1<UnityEngine.Renderer> CrowdMatch.PixelItem::renderers
	List_1_tD435DCC2A88E36DFC551EA5392CE0182F0C50E93* ___renderers_7;
	// UnityEngine.Animator CrowdMatch.PixelItem::animator
	Animator_t8A52E42AE54F76681838FE9E632683EF3952E883* ___animator_8;
	// UnityEngine.Transform CrowdMatch.PixelItem::exposeMoveTarget
	Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* ___exposeMoveTarget_9;
	// System.Single CrowdMatch.PixelItem::exposeMoveDuration
	float ___exposeMoveDuration_10;
	// CrowdMatch.PixelClickListener CrowdMatch.PixelItem::listener
	PixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524* ___listener_11;
	// System.Boolean CrowdMatch.PixelItem::<IsExposed>k__BackingField
	bool ___U3CIsExposedU3Ek__BackingField_12;
	// System.Boolean CrowdMatch.PixelItem::_wantWalking
	bool ____wantWalking_15;
	// System.Boolean CrowdMatch.PixelItem::_smoothing
	bool ____smoothing_16;
	// System.Single CrowdMatch.PixelItem::_restLocalY
	float ____restLocalY_17;
	// UnityEngine.Coroutine CrowdMatch.PixelItem::_exposeMove
	Coroutine_t85EA685566A254C23F3FD77AB5BDFFFF8799596B* ____exposeMove_18;
	// CrowdMatch.PixelGroup CrowdMatch.PixelItem::group
	PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* ___group_19;
	// System.Boolean CrowdMatch.PixelItem::arrivedAtGatherPoint
	bool ___arrivedAtGatherPoint_20;
	// System.Single CrowdMatch.PixelItem::bufferCrowdSpeed
	float ___bufferCrowdSpeed_21;
	// System.Single CrowdMatch.PixelItem::bufferAimOffset
	float ___bufferAimOffset_22;
};

// UnityEngine.EventSystems.UIBehaviour
struct UIBehaviour_tB9D4295827BD2EEDEF0749200C6CA7090C742A9D  : public MonoBehaviour_t532A11E69716D348D8AA7F854AFCBFCB8AD17F71
{
};

// CrowdMatch.WallItem
struct WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0  : public MonoBehaviour_t532A11E69716D348D8AA7F854AFCBFCB8AD17F71
{
	// System.Collections.Generic.List`1<UnityEngine.Vector2> CrowdMatch.WallItem::points
	List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B* ___points_4;
	// System.Single CrowdMatch.WallItem::height
	float ___height_5;
	// UnityEngine.Color CrowdMatch.WallItem::gizmoColor
	Color_tD001788D726C3A7F1379BEED0260B9591F440C1F ___gizmoColor_6;
	// CrowdMatch.PixelGroup CrowdMatch.WallItem::group
	PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* ___group_7;
	// CrowdMatch.PixelGroup CrowdMatch.WallItem::_gizmoGroup
	PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* ____gizmoGroup_8;
};

// UnityEngine.UI.Selectable
struct Selectable_t3251808068A17B8E92FB33590A4C2FA66D456712  : public UIBehaviour_tB9D4295827BD2EEDEF0749200C6CA7090C742A9D
{
	// System.Boolean UnityEngine.UI.Selectable::m_EnableCalled
	bool ___m_EnableCalled_6;
	// UnityEngine.UI.Navigation UnityEngine.UI.Selectable::m_Navigation
	Navigation_t4D2E201D65749CF4E104E8AC1232CF1D6F14795C ___m_Navigation_7;
	// UnityEngine.UI.Selectable/Transition UnityEngine.UI.Selectable::m_Transition
	int32_t ___m_Transition_8;
	// UnityEngine.UI.ColorBlock UnityEngine.UI.Selectable::m_Colors
	ColorBlock_tDD7C62E7AFE442652FC98F8D058CE8AE6BFD7C11 ___m_Colors_9;
	// UnityEngine.UI.SpriteState UnityEngine.UI.Selectable::m_SpriteState
	SpriteState_tC8199570BE6337FB5C49347C97892B4222E5AACD ___m_SpriteState_10;
	// UnityEngine.UI.AnimationTriggers UnityEngine.UI.Selectable::m_AnimationTriggers
	AnimationTriggers_tA0DC06F89C5280C6DD972F6F4C8A56D7F4F79074* ___m_AnimationTriggers_11;
	// System.Boolean UnityEngine.UI.Selectable::m_Interactable
	bool ___m_Interactable_12;
	// UnityEngine.UI.Graphic UnityEngine.UI.Selectable::m_TargetGraphic
	Graphic_tCBFCA4585A19E2B75465AECFEAC43F4016BF7931* ___m_TargetGraphic_13;
	// System.Boolean UnityEngine.UI.Selectable::m_GroupsAllowInteraction
	bool ___m_GroupsAllowInteraction_14;
	// System.Int32 UnityEngine.UI.Selectable::m_CurrentIndex
	int32_t ___m_CurrentIndex_15;
	// System.Boolean UnityEngine.UI.Selectable::<isPointerInside>k__BackingField
	bool ___U3CisPointerInsideU3Ek__BackingField_16;
	// System.Boolean UnityEngine.UI.Selectable::<isPointerDown>k__BackingField
	bool ___U3CisPointerDownU3Ek__BackingField_17;
	// System.Boolean UnityEngine.UI.Selectable::<hasSelection>k__BackingField
	bool ___U3ChasSelectionU3Ek__BackingField_18;
	// System.Collections.Generic.List`1<UnityEngine.CanvasGroup> UnityEngine.UI.Selectable::m_CanvasGroupCache
	List_1_t2CDCA768E7F493F5EDEBC75AEB200FD621354E35* ___m_CanvasGroupCache_19;
};

struct Selectable_t3251808068A17B8E92FB33590A4C2FA66D456712_StaticFields
{
	// UnityEngine.UI.Selectable[] UnityEngine.UI.Selectable::s_Selectables
	SelectableU5BU5D_t4160E135F02A40F75A63F787D36F31FEC6FE91A9* ___s_Selectables_4;
	// System.Int32 UnityEngine.UI.Selectable::s_SelectableCount
	int32_t ___s_SelectableCount_5;
};

// UnityEngine.UI.Button
struct Button_t6786514A57F7AFDEE5431112FEA0CAB24F5AE098  : public Selectable_t3251808068A17B8E92FB33590A4C2FA66D456712
{
	// UnityEngine.UI.Button/ButtonClickedEvent UnityEngine.UI.Button::m_OnClick
	ButtonClickedEvent_t8EA72E90B3BD1392FB3B3EF167D5121C23569E4C* ___m_OnClick_20;
};
#ifdef __clang__
#pragma clang diagnostic pop
#endif
// System.Int32[]
struct Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C  : public RuntimeArray
{
	ALIGN_FIELD (8) int32_t m_Items[1];

	inline int32_t GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline int32_t* GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, int32_t value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
	}
	inline int32_t GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline int32_t* GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, int32_t value)
	{
		m_Items[index] = value;
	}
};
// UnityEngine.Color[]
struct ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389  : public RuntimeArray
{
	ALIGN_FIELD (8) Color_tD001788D726C3A7F1379BEED0260B9591F440C1F m_Items[1];

	inline Color_tD001788D726C3A7F1379BEED0260B9591F440C1F GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline Color_tD001788D726C3A7F1379BEED0260B9591F440C1F* GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, Color_tD001788D726C3A7F1379BEED0260B9591F440C1F value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
	}
	inline Color_tD001788D726C3A7F1379BEED0260B9591F440C1F GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline Color_tD001788D726C3A7F1379BEED0260B9591F440C1F* GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, Color_tD001788D726C3A7F1379BEED0260B9591F440C1F value)
	{
		m_Items[index] = value;
	}
};
// CrowdMatch.PixelItem[]
struct PixelItemU5BU5D_t48D4473FA8D417437EB43513F73EC0FD24CBD02F  : public RuntimeArray
{
	ALIGN_FIELD (8) PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* m_Items[1];

	inline PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E** GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
		Il2CppCodeGenWriteBarrier((void**)m_Items + index, (void*)value);
	}
	inline PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E** GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* value)
	{
		m_Items[index] = value;
		Il2CppCodeGenWriteBarrier((void**)m_Items + index, (void*)value);
	}
};
// CrowdMatch.WallItem[]
struct WallItemU5BU5D_tD1B1EF0B4FAE8376B92DB7B23ACCE192653C9B58  : public RuntimeArray
{
	ALIGN_FIELD (8) WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* m_Items[1];

	inline WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0** GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
		Il2CppCodeGenWriteBarrier((void**)m_Items + index, (void*)value);
	}
	inline WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0** GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* value)
	{
		m_Items[index] = value;
		Il2CppCodeGenWriteBarrier((void**)m_Items + index, (void*)value);
	}
};
// CrowdMatch.PixelItem[,]
struct PixelItemU5BU2CU5D_t26ACEBF9031979698453CA52C61A20C0A1A68C7F  : public RuntimeArray
{
	ALIGN_FIELD (8) PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* m_Items[1];

	inline PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E** GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
		Il2CppCodeGenWriteBarrier((void**)m_Items + index, (void*)value);
	}
	inline PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E** GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* value)
	{
		m_Items[index] = value;
		Il2CppCodeGenWriteBarrier((void**)m_Items + index, (void*)value);
	}
	inline PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* GetAt(il2cpp_array_size_t i, il2cpp_array_size_t j) const
	{
		il2cpp_array_size_t iBound = bounds[0].length;
		IL2CPP_ARRAY_BOUNDS_CHECK(i, iBound);
		il2cpp_array_size_t jBound = bounds[1].length;
		IL2CPP_ARRAY_BOUNDS_CHECK(j, jBound);

		il2cpp_array_size_t index = i * jBound + j;
		return m_Items[index];
	}
	inline PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E** GetAddressAt(il2cpp_array_size_t i, il2cpp_array_size_t j)
	{
		il2cpp_array_size_t iBound = bounds[0].length;
		IL2CPP_ARRAY_BOUNDS_CHECK(i, iBound);
		il2cpp_array_size_t jBound = bounds[1].length;
		IL2CPP_ARRAY_BOUNDS_CHECK(j, jBound);

		il2cpp_array_size_t index = i * jBound + j;
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t i, il2cpp_array_size_t j, PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* value)
	{
		il2cpp_array_size_t iBound = bounds[0].length;
		IL2CPP_ARRAY_BOUNDS_CHECK(i, iBound);
		il2cpp_array_size_t jBound = bounds[1].length;
		IL2CPP_ARRAY_BOUNDS_CHECK(j, jBound);

		il2cpp_array_size_t index = i * jBound + j;
		m_Items[index] = value;
		Il2CppCodeGenWriteBarrier((void**)m_Items + index, (void*)value);
	}
	inline PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* GetAtUnchecked(il2cpp_array_size_t i, il2cpp_array_size_t j) const
	{
		il2cpp_array_size_t jBound = bounds[1].length;

		il2cpp_array_size_t index = i * jBound + j;
		return m_Items[index];
	}
	inline PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E** GetAddressAtUnchecked(il2cpp_array_size_t i, il2cpp_array_size_t j)
	{
		il2cpp_array_size_t jBound = bounds[1].length;

		il2cpp_array_size_t index = i * jBound + j;
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t i, il2cpp_array_size_t j, PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* value)
	{
		il2cpp_array_size_t jBound = bounds[1].length;

		il2cpp_array_size_t index = i * jBound + j;
		m_Items[index] = value;
		Il2CppCodeGenWriteBarrier((void**)m_Items + index, (void*)value);
	}
};
// System.Boolean[,]
struct BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6  : public RuntimeArray
{
	ALIGN_FIELD (8) bool m_Items[1];

	inline bool GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline bool* GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, bool value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
	}
	inline bool GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline bool* GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, bool value)
	{
		m_Items[index] = value;
	}
	inline bool GetAt(il2cpp_array_size_t i, il2cpp_array_size_t j) const
	{
		il2cpp_array_size_t iBound = bounds[0].length;
		IL2CPP_ARRAY_BOUNDS_CHECK(i, iBound);
		il2cpp_array_size_t jBound = bounds[1].length;
		IL2CPP_ARRAY_BOUNDS_CHECK(j, jBound);

		il2cpp_array_size_t index = i * jBound + j;
		return m_Items[index];
	}
	inline bool* GetAddressAt(il2cpp_array_size_t i, il2cpp_array_size_t j)
	{
		il2cpp_array_size_t iBound = bounds[0].length;
		IL2CPP_ARRAY_BOUNDS_CHECK(i, iBound);
		il2cpp_array_size_t jBound = bounds[1].length;
		IL2CPP_ARRAY_BOUNDS_CHECK(j, jBound);

		il2cpp_array_size_t index = i * jBound + j;
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t i, il2cpp_array_size_t j, bool value)
	{
		il2cpp_array_size_t iBound = bounds[0].length;
		IL2CPP_ARRAY_BOUNDS_CHECK(i, iBound);
		il2cpp_array_size_t jBound = bounds[1].length;
		IL2CPP_ARRAY_BOUNDS_CHECK(j, jBound);

		il2cpp_array_size_t index = i * jBound + j;
		m_Items[index] = value;
	}
	inline bool GetAtUnchecked(il2cpp_array_size_t i, il2cpp_array_size_t j) const
	{
		il2cpp_array_size_t jBound = bounds[1].length;

		il2cpp_array_size_t index = i * jBound + j;
		return m_Items[index];
	}
	inline bool* GetAddressAtUnchecked(il2cpp_array_size_t i, il2cpp_array_size_t j)
	{
		il2cpp_array_size_t jBound = bounds[1].length;

		il2cpp_array_size_t index = i * jBound + j;
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t i, il2cpp_array_size_t j, bool value)
	{
		il2cpp_array_size_t jBound = bounds[1].length;

		il2cpp_array_size_t index = i * jBound + j;
		m_Items[index] = value;
	}
};
// System.String[]
struct StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248  : public RuntimeArray
{
	ALIGN_FIELD (8) String_t* m_Items[1];

	inline String_t* GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline String_t** GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, String_t* value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
		Il2CppCodeGenWriteBarrier((void**)m_Items + index, (void*)value);
	}
	inline String_t* GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline String_t** GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, String_t* value)
	{
		m_Items[index] = value;
		Il2CppCodeGenWriteBarrier((void**)m_Items + index, (void*)value);
	}
};
// UnityEngine.Vector2Int[]
struct Vector2IntU5BU5D_tF9E2BDAC11B246DF7EEB9137B826A0CBEBD59534  : public RuntimeArray
{
	ALIGN_FIELD (8) Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A m_Items[1];

	inline Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A* GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
	}
	inline Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A* GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A value)
	{
		m_Items[index] = value;
	}
};
// System.ValueTuple`3<System.Int32,System.Int32,System.Int32>[]
struct ValueTuple_3U5BU5D_t2EE684CE383C822F20AF09A2B0A3C10C5A8A94E5  : public RuntimeArray
{
	ALIGN_FIELD (8) ValueTuple_3_tBFE24970AFB182B3C77F4C747E3F1E7A7B4EBC57 m_Items[1];

	inline ValueTuple_3_tBFE24970AFB182B3C77F4C747E3F1E7A7B4EBC57 GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline ValueTuple_3_tBFE24970AFB182B3C77F4C747E3F1E7A7B4EBC57* GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, ValueTuple_3_tBFE24970AFB182B3C77F4C747E3F1E7A7B4EBC57 value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
	}
	inline ValueTuple_3_tBFE24970AFB182B3C77F4C747E3F1E7A7B4EBC57 GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline ValueTuple_3_tBFE24970AFB182B3C77F4C747E3F1E7A7B4EBC57* GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, ValueTuple_3_tBFE24970AFB182B3C77F4C747E3F1E7A7B4EBC57 value)
	{
		m_Items[index] = value;
	}
};


// T UnityEngine.GameObject::GetComponent<System.Object>()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* GameObject_GetComponent_TisRuntimeObject_m6EAED4AA356F0F48288F67899E5958792395563B_gshared (GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* __this, const RuntimeMethod* method) ;
// T UnityEngine.Component::GetComponent<System.Object>()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* Component_GetComponent_TisRuntimeObject_m7181F81CAEC2CF53F5D2BC79B7425C16E1F80D33_gshared (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* __this, const RuntimeMethod* method) ;
// T[] UnityEngine.Component::GetComponentsInChildren<System.Object>()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ObjectU5BU5D_t8061030B0A12A55D5AD8652A20C922FE99450918* Component_GetComponentsInChildren_TisRuntimeObject_m1F5B6FC0689B07D4FAAC0C605D9B2933A9B32543_gshared (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* __this, const RuntimeMethod* method) ;
// System.Void System.Collections.Generic.List`1<UnityEngine.Vector2Int>::.ctor()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void List_1__ctor_mE1D9FD9DA1EF2CAC4F99EF4E013F05BB8C3507EF_gshared (List_1_tB56F1028A724D2CE4E84861619D1CF68C68C983D* __this, const RuntimeMethod* method) ;
// System.Void System.Collections.Generic.Queue`1<UnityEngine.Vector2Int>::.ctor()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Queue_1__ctor_m262194FC7D76E5DB95E022130A30E64125C0A90E_gshared (Queue_1_t9B96DF949ACABD4B0F595738FC87DC80BCCC3875* __this, const RuntimeMethod* method) ;
// System.Void System.Collections.Generic.Queue`1<UnityEngine.Vector2Int>::Enqueue(T)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Queue_1_Enqueue_m8507576A07092ADD61289B4C7F248A6C94944EFB_gshared (Queue_1_t9B96DF949ACABD4B0F595738FC87DC80BCCC3875* __this, Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A ___item0, const RuntimeMethod* method) ;
// T System.Collections.Generic.Queue`1<UnityEngine.Vector2Int>::Dequeue()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A Queue_1_Dequeue_mD30377AD154F6A542F280578B393102A1D5378EB_gshared (Queue_1_t9B96DF949ACABD4B0F595738FC87DC80BCCC3875* __this, const RuntimeMethod* method) ;
// System.Void System.Collections.Generic.List`1<UnityEngine.Vector2Int>::Add(T)
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void List_1_Add_m771AC7A01DFC931CCCFCCF949C1F4D56B5E98A1B_gshared_inline (List_1_tB56F1028A724D2CE4E84861619D1CF68C68C983D* __this, Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A ___item0, const RuntimeMethod* method) ;
// System.Int32 System.Collections.Generic.Queue`1<UnityEngine.Vector2Int>::get_Count()
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t Queue_1_get_Count_m1FE2DD00C23DF83E6DBC5BE6A23A9FE7FBF772DD_gshared_inline (Queue_1_t9B96DF949ACABD4B0F595738FC87DC80BCCC3875* __this, const RuntimeMethod* method) ;
// System.Collections.Generic.List`1/Enumerator<T> System.Collections.Generic.List`1<UnityEngine.Vector2Int>::GetEnumerator()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Enumerator_t4BD1B5394A2E9960562CC364C2CCE4FCBD1AC258 List_1_GetEnumerator_m039302BD172C3288503DB73B6E2B27B8D8BC0321_gshared (List_1_tB56F1028A724D2CE4E84861619D1CF68C68C983D* __this, const RuntimeMethod* method) ;
// System.Void System.Collections.Generic.List`1/Enumerator<UnityEngine.Vector2Int>::Dispose()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Enumerator_Dispose_m2A96F62698864FA1E73292450EBF2019F7104BBC_gshared (Enumerator_t4BD1B5394A2E9960562CC364C2CCE4FCBD1AC258* __this, const RuntimeMethod* method) ;
// T System.Collections.Generic.List`1/Enumerator<UnityEngine.Vector2Int>::get_Current()
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A Enumerator_get_Current_m87245A61572727EBCD1642C4A2BD99B11CE9FA8A_gshared_inline (Enumerator_t4BD1B5394A2E9960562CC364C2CCE4FCBD1AC258* __this, const RuntimeMethod* method) ;
// System.Boolean System.Collections.Generic.List`1/Enumerator<UnityEngine.Vector2Int>::MoveNext()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Enumerator_MoveNext_mD6D16710D40F62D081A4973E4D8CA1614D1482B5_gshared (Enumerator_t4BD1B5394A2E9960562CC364C2CCE4FCBD1AC258* __this, const RuntimeMethod* method) ;
// T UnityEngine.GameObject::AddComponent<System.Object>()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* GameObject_AddComponent_TisRuntimeObject_m69B93700FACCF372F5753371C6E8FB780800B824_gshared (GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* __this, const RuntimeMethod* method) ;
// System.Void System.Collections.Generic.List`1<UnityEngine.Vector2>::.ctor(System.Collections.Generic.IEnumerable`1<T>)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void List_1__ctor_m105596C2159C46B75E96D26ACEC0A5C1C1F5C5EC_gshared (List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B* __this, RuntimeObject* ___collection0, const RuntimeMethod* method) ;
// T UnityEngine.Object::Instantiate<System.Object>(T)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* Object_Instantiate_TisRuntimeObject_m90A1E6C4C2B445D2E848DB75C772D1B95AAC046A_gshared (RuntimeObject* ___original0, const RuntimeMethod* method) ;
// T UnityEngine.Component::GetComponentInChildren<System.Object>(System.Boolean)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* Component_GetComponentInChildren_TisRuntimeObject_m831BC1785A9E9CB99F8D66BDFCF6D606622B5ADB_gshared (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* __this, bool ___includeInactive0, const RuntimeMethod* method) ;
// T DG.Tweening.TweenSettingsExtensions::OnComplete<System.Object>(T,DG.Tweening.TweenCallback)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* TweenSettingsExtensions_OnComplete_TisRuntimeObject_mC014D07E92193DA79B257C4508B6DF208FE502A6_gshared (RuntimeObject* ___t0, TweenCallback_t7C8B8A38E7B30905FF1B83C943256EF23617BB24* ___action1, const RuntimeMethod* method) ;
// System.Collections.Generic.List`1/Enumerator<T> System.Collections.Generic.List`1<System.Object>::GetEnumerator()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Enumerator_t9473BAB568A27E2339D48C1F91319E0F6D244D7A List_1_GetEnumerator_mD8294A7FA2BEB1929487127D476F8EC1CDC23BFC_gshared (List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D* __this, const RuntimeMethod* method) ;
// System.Void System.Collections.Generic.List`1/Enumerator<System.Object>::Dispose()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Enumerator_Dispose_mD9DC3E3C3697830A4823047AB29A77DBBB5ED419_gshared (Enumerator_t9473BAB568A27E2339D48C1F91319E0F6D244D7A* __this, const RuntimeMethod* method) ;
// T System.Collections.Generic.List`1/Enumerator<System.Object>::get_Current()
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR RuntimeObject* Enumerator_get_Current_m6330F15D18EE4F547C05DF9BF83C5EB710376027_gshared_inline (Enumerator_t9473BAB568A27E2339D48C1F91319E0F6D244D7A* __this, const RuntimeMethod* method) ;
// System.Boolean System.Collections.Generic.List`1/Enumerator<System.Object>::MoveNext()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Enumerator_MoveNext_mE921CC8F29FBBDE7CC3209A0ED0D921D58D00BCB_gshared (Enumerator_t9473BAB568A27E2339D48C1F91319E0F6D244D7A* __this, const RuntimeMethod* method) ;
// System.Void System.Collections.Generic.List`1<System.Object>::.ctor()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void List_1__ctor_m7F078BB342729BDF11327FD89D7872265328F690_gshared (List_1_tA239CB83DE5615F348BB0507E45F490F4F7C9A8D* __this, const RuntimeMethod* method) ;
// System.Void System.Collections.Generic.List`1<System.ValueTuple`3<System.Int32,System.Int32,System.Int32>>::.ctor()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void List_1__ctor_m7734609AB0CE32B017F3FBC89E3A0D35323A5015_gshared (List_1_t264DA9C475A0E3CF57148457209560CF2E2B6F27* __this, const RuntimeMethod* method) ;
// TResult System.Func`3<System.Int32,System.Int32,System.Boolean>::Invoke(T1,T2)
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR bool Func_3_Invoke_m5C4CCADFF1AE4540F252182089A9BF3CBE7BAFE6_gshared_inline (Func_3_tE8F85DA3CAC4998201E5C56356280AFAB7185B69* __this, int32_t ___arg10, int32_t ___arg21, const RuntimeMethod* method) ;
// System.Void System.ValueTuple`3<System.Int32,System.Int32,System.Int32>::.ctor(T1,T2,T3)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void ValueTuple_3__ctor_m0D7E698F23721325245996D0B8DED1C102559F60_gshared (ValueTuple_3_tBFE24970AFB182B3C77F4C747E3F1E7A7B4EBC57* __this, int32_t ___item10, int32_t ___item21, int32_t ___item32, const RuntimeMethod* method) ;
// System.Void System.Collections.Generic.List`1<System.ValueTuple`3<System.Int32,System.Int32,System.Int32>>::Add(T)
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void List_1_Add_m716377944B1E88CBB1B269AA9CF38C525A41D367_gshared_inline (List_1_t264DA9C475A0E3CF57148457209560CF2E2B6F27* __this, ValueTuple_3_tBFE24970AFB182B3C77F4C747E3F1E7A7B4EBC57 ___item0, const RuntimeMethod* method) ;
// System.Boolean System.Nullable`1<UnityEngine.Color>::get_HasValue()
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR bool Nullable_1_get_HasValue_mFAF0B4EEA878E596C80258FE3BDA57CEF40C8D7F_gshared_inline (Nullable_1_tEE83D90B507D40B6C58B5EEF5B9D44D377B44F11* __this, const RuntimeMethod* method) ;
// T System.Nullable`1<UnityEngine.Color>::GetValueOrDefault()
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Color_tD001788D726C3A7F1379BEED0260B9591F440C1F Nullable_1_GetValueOrDefault_m9A7869C021F041D45F2A851F70F97F8114AC99E4_gshared_inline (Nullable_1_tEE83D90B507D40B6C58B5EEF5B9D44D377B44F11* __this, const RuntimeMethod* method) ;
// TResult System.Func`3<System.Int32,System.Int32,System.Nullable`1<UnityEngine.Color>>::Invoke(T1,T2)
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Nullable_1_tEE83D90B507D40B6C58B5EEF5B9D44D377B44F11 Func_3_Invoke_mADC4326AE0426011BAE02568945B03277B225B79_gshared_inline (Func_3_t171A20B65D1078C91C9A875EF6CBB2716C7E322D* __this, int32_t ___arg10, int32_t ___arg21, const RuntimeMethod* method) ;
// T System.Nullable`1<UnityEngine.Color>::get_Value()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Color_tD001788D726C3A7F1379BEED0260B9591F440C1F Nullable_1_get_Value_m3FC15B40E747AA6A9F2AB10A00C46ABE84393637_gshared (Nullable_1_tEE83D90B507D40B6C58B5EEF5B9D44D377B44F11* __this, const RuntimeMethod* method) ;
// T UnityEngine.Component::GetComponentInParent<System.Object>()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* Component_GetComponentInParent_TisRuntimeObject_m6746D6BB99912B1B509746C993906492F86CD119_gshared (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* __this, const RuntimeMethod* method) ;
// T System.Collections.Generic.List`1<UnityEngine.Vector2>::get_Item(System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 List_1_get_Item_m1F8E226CAD72B83C5E75BB66B43025247806B543_gshared (List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B* __this, int32_t ___index0, const RuntimeMethod* method) ;
// System.Int32 System.Collections.Generic.List`1<UnityEngine.Vector2>::get_Count()
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t List_1_get_Count_mF1C0C56457C655BDFFC6EE5B46FAD8BAEC1F588B_gshared_inline (List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B* __this, const RuntimeMethod* method) ;
// System.Boolean System.Collections.Generic.HashSet`1<UnityEngine.Vector2Int>::Add(T)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool HashSet_1_Add_m517B9238E386530A854B4286891358CC8327A7B6_gshared (HashSet_1_t55B946E0B889BDE32B3004D0822A6C5017AFA406* __this, Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A ___item0, const RuntimeMethod* method) ;
// System.Void System.Collections.Generic.HashSet`1<UnityEngine.Vector2Int>::.ctor()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void HashSet_1__ctor_m0E27874668BB3B3160062D69799276CFEF8072AB_gshared (HashSet_1_t55B946E0B889BDE32B3004D0822A6C5017AFA406* __this, const RuntimeMethod* method) ;
// System.Void System.Collections.Generic.List`1<UnityEngine.Vector2>::.ctor()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void List_1__ctor_m88C4BD8AC607DB3585552068F4DC437406358D5F_gshared (List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B* __this, const RuntimeMethod* method) ;

// System.Void System.Object::.ctor()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2 (RuntimeObject* __this, const RuntimeMethod* method) ;
// System.Int32 System.Environment::get_CurrentManagedThreadId()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t Environment_get_CurrentManagedThreadId_m66483AADCCC13272EBDCD94D31D2E52603C24BDF (const RuntimeMethod* method) ;
// CrowdMatch.PixelItem CrowdMatch.PixelGroup::GetItem(System.Int32,System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* PixelGroup_GetItem_mDD0EEC5AAF354352C1CC8B66BE3C59FD78F4B8DE (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, int32_t ___col0, int32_t ___row1, const RuntimeMethod* method) ;
// System.Boolean UnityEngine.Object::op_Inequality(UnityEngine.Object,UnityEngine.Object)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602 (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C* ___x0, Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C* ___y1, const RuntimeMethod* method) ;
// System.Void System.NotSupportedException::.ctor()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void NotSupportedException__ctor_m1398D0CDE19B36AA3DE9392879738C1EA2439CDF (NotSupportedException_t1429765983D409BD2986508963C98D214E4EBF4A* __this, const RuntimeMethod* method) ;
// System.Void CrowdMatch.GameController/<GetNeighbors>d__43::.ctor(System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void U3CGetNeighborsU3Ed__43__ctor_m715F3C12C7FEB137BF9A1396437FD0D1D2120204 (U3CGetNeighborsU3Ed__43_t7EE77D676072170DE792051688D0316271D3643C* __this, int32_t ___U3CU3E1__state0, const RuntimeMethod* method) ;
// System.Collections.Generic.IEnumerator`1<CrowdMatch.PixelItem> CrowdMatch.GameController/<GetNeighbors>d__43::System.Collections.Generic.IEnumerable<CrowdMatch.PixelItem>.GetEnumerator()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* U3CGetNeighborsU3Ed__43_System_Collections_Generic_IEnumerableU3CCrowdMatch_PixelItemU3E_GetEnumerator_mE5B4928F05E765D5395B3FF53C3356C75A8B975F (U3CGetNeighborsU3Ed__43_t7EE77D676072170DE792051688D0316271D3643C* __this, const RuntimeMethod* method) ;
// UnityEngine.Transform UnityEngine.Component::get_transform()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* Component_get_transform_m2919A1D81931E6932C7F06D4C2F0AB8DDA9A5371 (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* __this, const RuntimeMethod* method) ;
// UnityEngine.Vector3 UnityEngine.Transform::get_localPosition()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Transform_get_localPosition_mA9C86B990DF0685EA1061A120218993FDCC60A95 (Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* __this, const RuntimeMethod* method) ;
// UnityEngine.Vector3 CrowdMatch.GameController::RandomGatherTarget()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 GameController_RandomGatherTarget_m44DC3062E3FCE4FE5F9416A9CA86E46DB2EF786C (GameController_t9B394943D9DA551993B8515B21F692D9B0E00853* __this, const RuntimeMethod* method) ;
// System.Single UnityEngine.Vector3::Distance(UnityEngine.Vector3,UnityEngine.Vector3)
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float Vector3_Distance_m2314DB9B8BD01157E013DF87BEA557375C7F9FF9_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___a0, Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___b1, const RuntimeMethod* method) ;
// System.Single UnityEngine.Time::get_deltaTime()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float Time_get_deltaTime_mC3195000401F0FD167DD2F948FD2BC58330D0865 (const RuntimeMethod* method) ;
// System.Single UnityEngine.Mathf::Clamp01(System.Single)
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float Mathf_Clamp01_mA7E048DBDA832D399A581BE4D6DED9FA44CE0F14_inline (float ___value0, const RuntimeMethod* method) ;
// UnityEngine.Vector3 UnityEngine.Vector3::Lerp(UnityEngine.Vector3,UnityEngine.Vector3,System.Single)
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_Lerp_m3A906D0530A94FAABB94F0F905E84D99BE85C3F8_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___a0, Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___b1, float ___t2, const RuntimeMethod* method) ;
// System.Void UnityEngine.Transform::set_localPosition(UnityEngine.Vector3)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Transform_set_localPosition_mDE1C997F7D79C0885210B7732B4BA50EE7D73134 (Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* __this, Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___value0, const RuntimeMethod* method) ;
// System.Void CrowdMatch.PixelItem::SetWalking(System.Boolean)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelItem_SetWalking_mEA83F37D0924BFD82287B01679016B81CAB96C50 (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, bool ___walking0, const RuntimeMethod* method) ;
// System.Boolean UnityEngine.Mathf::Approximately(System.Single,System.Single)
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR bool Mathf_Approximately_m1DADD012A8FC82E11FB282501AE2EBBF9A77150B_inline (float ___a0, float ___b1, const RuntimeMethod* method) ;
// System.Boolean UnityEngine.Object::op_Equality(UnityEngine.Object,UnityEngine.Object)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605 (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C* ___x0, Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C* ___y1, const RuntimeMethod* method) ;
// UnityEngine.UI.Button CrowdMatch.LevelSkipButtons::FindButton(System.String)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Button_t6786514A57F7AFDEE5431112FEA0CAB24F5AE098* LevelSkipButtons_FindButton_m45658843F17679CBF404FFC1B70ABC044427D53F (String_t* ___name0, const RuntimeMethod* method) ;
// UnityEngine.UI.Button/ButtonClickedEvent UnityEngine.UI.Button::get_onClick()
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR ButtonClickedEvent_t8EA72E90B3BD1392FB3B3EF167D5121C23569E4C* Button_get_onClick_m701712A7F7F000CC80D517C4510697E15722C35C_inline (Button_t6786514A57F7AFDEE5431112FEA0CAB24F5AE098* __this, const RuntimeMethod* method) ;
// System.Void UnityEngine.Events.UnityAction::.ctor(System.Object,System.IntPtr)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void UnityAction__ctor_mC53E20D6B66E0D5688CD81B88DBB34F5A58B7131 (UnityAction_t11A1F3B953B365C072A5DCC32677EE1796A962A7* __this, RuntimeObject* ___object0, intptr_t ___method1, const RuntimeMethod* method) ;
// System.Void UnityEngine.Events.UnityEvent::AddListener(UnityEngine.Events.UnityAction)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void UnityEvent_AddListener_m8AA4287C16628486B41DA41CA5E7A856A706D302 (UnityEvent_tDC2C3548799DBC91D1E3F3DE60083A66F4751977* __this, UnityAction_t11A1F3B953B365C072A5DCC32677EE1796A962A7* ___call0, const RuntimeMethod* method) ;
// UnityEngine.GameObject UnityEngine.GameObject::Find(System.String)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* GameObject_Find_m7A669B4EEC2617AB82F6E3FF007CDCD9F21DB300 (String_t* ___name0, const RuntimeMethod* method) ;
// T UnityEngine.GameObject::GetComponent<UnityEngine.UI.Button>()
inline Button_t6786514A57F7AFDEE5431112FEA0CAB24F5AE098* GameObject_GetComponent_TisButton_t6786514A57F7AFDEE5431112FEA0CAB24F5AE098_mB997CBF78A37938DC1624352E12D0205078CB290 (GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* __this, const RuntimeMethod* method)
{
	return ((  Button_t6786514A57F7AFDEE5431112FEA0CAB24F5AE098* (*) (GameObject_t76FEDD663AB33C991A9C9A23129337651094216F*, const RuntimeMethod*))GameObject_GetComponent_TisRuntimeObject_m6EAED4AA356F0F48288F67899E5958792395563B_gshared)(__this, method);
}
// CrowdMatch.GameManager CrowdMatch.GameManager::get_Instance()
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR GameManager_t5B23045F478AD8B0E3D943ECC8493384B02FF32F* GameManager_get_Instance_m5F7736EF916BFD34C734BE27B0EA4760C2D545FA_inline (const RuntimeMethod* method) ;
// System.Void CrowdMatch.GameManager::GameWin()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void GameManager_GameWin_m65A28284A87BCEEAE65E48A161A1C5D81B71B9A4 (GameManager_t5B23045F478AD8B0E3D943ECC8493384B02FF32F* __this, const RuntimeMethod* method) ;
// System.Void CrowdMatch.GameManager::PrevLevel()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void GameManager_PrevLevel_m53AEC4E3BF586BAAF764D5F175322FB30F17FCFC (GameManager_t5B23045F478AD8B0E3D943ECC8493384B02FF32F* __this, const RuntimeMethod* method) ;
// System.Void UnityEngine.MonoBehaviour::.ctor()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void MonoBehaviour__ctor_m592DB0105CA0BC97AA1C5F4AD27B12D68A3B7C1E (MonoBehaviour_t532A11E69716D348D8AA7F854AFCBFCB8AD17F71* __this, const RuntimeMethod* method) ;
// T UnityEngine.Component::GetComponent<UnityEngine.BoxCollider>()
inline BoxCollider_tFA5D239388334D6DE0B8FFDAD6825C5B03786E23* Component_GetComponent_TisBoxCollider_tFA5D239388334D6DE0B8FFDAD6825C5B03786E23_m59698092F1230C6FB7F40D0F58F643A931A732D7 (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* __this, const RuntimeMethod* method)
{
	return ((  BoxCollider_tFA5D239388334D6DE0B8FFDAD6825C5B03786E23* (*) (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3*, const RuntimeMethod*))Component_GetComponent_TisRuntimeObject_m7181F81CAEC2CF53F5D2BC79B7425C16E1F80D33_gshared)(__this, method);
}
// System.Void CrowdMatch.PixelClickListener::set_BoxCollider(UnityEngine.BoxCollider)
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void PixelClickListener_set_BoxCollider_m103128AD33A18949AC8C92230E2451E44A68D847_inline (PixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524* __this, BoxCollider_tFA5D239388334D6DE0B8FFDAD6825C5B03786E23* ___value0, const RuntimeMethod* method) ;
// UnityEngine.BoxCollider CrowdMatch.PixelClickListener::get_BoxCollider()
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR BoxCollider_tFA5D239388334D6DE0B8FFDAD6825C5B03786E23* PixelClickListener_get_BoxCollider_m9BC4090DD0238F70A3BD78FA280683C138C66C63_inline (PixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524* __this, const RuntimeMethod* method) ;
// System.Void UnityEngine.Collider::set_enabled(System.Boolean)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Collider_set_enabled_m8D5C3B5047592D227A52560FC9723D176E209F70 (Collider_t1CC3163924FCD6C4CC2E816373A929C1E3D55E76* __this, bool ___value0, const RuntimeMethod* method) ;
// System.Int32 UnityEngine.Mathf::Max(System.Int32,System.Int32)
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t Mathf_Max_m7FA442918DE37E3A00106D1F2E789D65829792B8_inline (int32_t ___a0, int32_t ___b1, const RuntimeMethod* method) ;
// System.Void CrowdMatch.PixelGroup::RebuildGrid()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelGroup_RebuildGrid_mDE9E7C6B2933B5C2FDBCE71287AA8ECC2D2510E1 (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, const RuntimeMethod* method) ;
// System.Int32 CrowdMatch.PixelGroup::get_TotalRows()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t PixelGroup_get_TotalRows_m463568B781F3699B24713B03D51CB2A5084E2D79 (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, const RuntimeMethod* method) ;
// T[] UnityEngine.Component::GetComponentsInChildren<CrowdMatch.PixelItem>()
inline PixelItemU5BU5D_t48D4473FA8D417437EB43513F73EC0FD24CBD02F* Component_GetComponentsInChildren_TisPixelItem_t863890C77945A8C08435BF2F8859A47A413C794E_mC6DFA6AEA610A79B3113B66C8457AA3325D29586 (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* __this, const RuntimeMethod* method)
{
	return ((  PixelItemU5BU5D_t48D4473FA8D417437EB43513F73EC0FD24CBD02F* (*) (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3*, const RuntimeMethod*))Component_GetComponentsInChildren_TisRuntimeObject_m1F5B6FC0689B07D4FAAC0C605D9B2933A9B32543_gshared)(__this, method);
}
// System.Boolean CrowdMatch.PixelGroup::IsInRange(System.Int32,System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool PixelGroup_IsInRange_m7D51A009EE60A9ADDC0707368ADDDE95FCF788C3 (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, int32_t ___col0, int32_t ___row1, const RuntimeMethod* method) ;
// T[] UnityEngine.Component::GetComponentsInChildren<CrowdMatch.WallItem>()
inline WallItemU5BU5D_tD1B1EF0B4FAE8376B92DB7B23ACCE192653C9B58* Component_GetComponentsInChildren_TisWallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0_m9AD00368CDD148B6F031F3195DAF4512127EBA59 (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* __this, const RuntimeMethod* method)
{
	return ((  WallItemU5BU5D_tD1B1EF0B4FAE8376B92DB7B23ACCE192653C9B58* (*) (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3*, const RuntimeMethod*))Component_GetComponentsInChildren_TisRuntimeObject_m1F5B6FC0689B07D4FAAC0C605D9B2933A9B32543_gshared)(__this, method);
}
// System.Collections.Generic.IEnumerable`1<UnityEngine.Vector2Int> CrowdMatch.WallItem::EnumerateOccupiedCells()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* WallItem_EnumerateOccupiedCells_mA15C2F1FBAAE6D7E2F190B8396AF069D35D717BF (WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* __this, const RuntimeMethod* method) ;
// System.Int32 UnityEngine.Vector2Int::get_x()
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t Vector2Int_get_x_mA2CACB1B6E6B5AD0CCC32B2CD2EDCE3ECEB50576_inline (Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A* __this, const RuntimeMethod* method) ;
// System.Int32 UnityEngine.Vector2Int::get_y()
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t Vector2Int_get_y_m48454163ECF0B463FB5A16A0C4FC4B14DB0768B3_inline (Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A* __this, const RuntimeMethod* method) ;
// System.Boolean CrowdMatch.PixelGroup::IsWall(System.Int32,System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool PixelGroup_IsWall_m2680CC2BF1BAB1591F9AC9C7D56E714047AE9B87 (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, int32_t ___col0, int32_t ___row1, const RuntimeMethod* method) ;
// System.Single CrowdMatch.PixelGroup::get_CellSizeX()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PixelGroup_get_CellSizeX_mFF4EC1FE8D75565520456EB7BD12D9DFCF2AFAE0 (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, const RuntimeMethod* method) ;
// System.Single CrowdMatch.PixelGroup::get_CellSizeZ()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PixelGroup_get_CellSizeZ_m8BD98A189CFE63A89D5928CDE4EB4DD7D681CC50 (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, const RuntimeMethod* method) ;
// System.Void UnityEngine.Vector3::.ctor(System.Single,System.Single,System.Single)
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Vector3__ctor_m376936E6B999EF1ECBE57D990A386303E2283DE0_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2* __this, float ___x0, float ___y1, float ___z2, const RuntimeMethod* method) ;
// UnityEngine.Vector3 CrowdMatch.PixelGroup::GetLocalPosition(System.Int32,System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 PixelGroup_GetLocalPosition_mA20E329BB838AB5B672C2A969C0E5B52A1771DD3 (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, int32_t ___col0, int32_t ___row1, const RuntimeMethod* method) ;
// UnityEngine.Vector3 UnityEngine.Transform::TransformPoint(UnityEngine.Vector3)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Transform_TransformPoint_m05BFF013DB830D7BFE44A007703694AE1062EE44 (Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* __this, Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___position0, const RuntimeMethod* method) ;
// System.Boolean CrowdMatch.PixelGroup::IsEmpty(System.Int32,System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool PixelGroup_IsEmpty_m67995CF792BAD887755E45E0F0D2FEDECDE3F570 (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, int32_t ___col0, int32_t ___row1, const RuntimeMethod* method) ;
// System.Void System.Collections.Generic.List`1<UnityEngine.Vector2Int>::.ctor()
inline void List_1__ctor_mE1D9FD9DA1EF2CAC4F99EF4E013F05BB8C3507EF (List_1_tB56F1028A724D2CE4E84861619D1CF68C68C983D* __this, const RuntimeMethod* method)
{
	((  void (*) (List_1_tB56F1028A724D2CE4E84861619D1CF68C68C983D*, const RuntimeMethod*))List_1__ctor_mE1D9FD9DA1EF2CAC4F99EF4E013F05BB8C3507EF_gshared)(__this, method);
}
// System.Void System.Collections.Generic.Queue`1<UnityEngine.Vector2Int>::.ctor()
inline void Queue_1__ctor_m262194FC7D76E5DB95E022130A30E64125C0A90E (Queue_1_t9B96DF949ACABD4B0F595738FC87DC80BCCC3875* __this, const RuntimeMethod* method)
{
	((  void (*) (Queue_1_t9B96DF949ACABD4B0F595738FC87DC80BCCC3875*, const RuntimeMethod*))Queue_1__ctor_m262194FC7D76E5DB95E022130A30E64125C0A90E_gshared)(__this, method);
}
// System.Void UnityEngine.Vector2Int::.ctor(System.Int32,System.Int32)
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Vector2Int__ctor_mC20D1312133EB8CB63EC11067088B043660F11CE_inline (Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A* __this, int32_t ___x0, int32_t ___y1, const RuntimeMethod* method) ;
// System.Void System.Collections.Generic.Queue`1<UnityEngine.Vector2Int>::Enqueue(T)
inline void Queue_1_Enqueue_m8507576A07092ADD61289B4C7F248A6C94944EFB (Queue_1_t9B96DF949ACABD4B0F595738FC87DC80BCCC3875* __this, Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A ___item0, const RuntimeMethod* method)
{
	((  void (*) (Queue_1_t9B96DF949ACABD4B0F595738FC87DC80BCCC3875*, Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A, const RuntimeMethod*))Queue_1_Enqueue_m8507576A07092ADD61289B4C7F248A6C94944EFB_gshared)(__this, ___item0, method);
}
// T System.Collections.Generic.Queue`1<UnityEngine.Vector2Int>::Dequeue()
inline Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A Queue_1_Dequeue_mD30377AD154F6A542F280578B393102A1D5378EB (Queue_1_t9B96DF949ACABD4B0F595738FC87DC80BCCC3875* __this, const RuntimeMethod* method)
{
	return ((  Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A (*) (Queue_1_t9B96DF949ACABD4B0F595738FC87DC80BCCC3875*, const RuntimeMethod*))Queue_1_Dequeue_mD30377AD154F6A542F280578B393102A1D5378EB_gshared)(__this, method);
}
// System.Void System.Collections.Generic.List`1<UnityEngine.Vector2Int>::Add(T)
inline void List_1_Add_m771AC7A01DFC931CCCFCCF949C1F4D56B5E98A1B_inline (List_1_tB56F1028A724D2CE4E84861619D1CF68C68C983D* __this, Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A ___item0, const RuntimeMethod* method)
{
	((  void (*) (List_1_tB56F1028A724D2CE4E84861619D1CF68C68C983D*, Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A, const RuntimeMethod*))List_1_Add_m771AC7A01DFC931CCCFCCF949C1F4D56B5E98A1B_gshared_inline)(__this, ___item0, method);
}
// System.Int32 System.Collections.Generic.Queue`1<UnityEngine.Vector2Int>::get_Count()
inline int32_t Queue_1_get_Count_m1FE2DD00C23DF83E6DBC5BE6A23A9FE7FBF772DD_inline (Queue_1_t9B96DF949ACABD4B0F595738FC87DC80BCCC3875* __this, const RuntimeMethod* method)
{
	return ((  int32_t (*) (Queue_1_t9B96DF949ACABD4B0F595738FC87DC80BCCC3875*, const RuntimeMethod*))Queue_1_get_Count_m1FE2DD00C23DF83E6DBC5BE6A23A9FE7FBF772DD_gshared_inline)(__this, method);
}
// System.Collections.Generic.List`1/Enumerator<T> System.Collections.Generic.List`1<UnityEngine.Vector2Int>::GetEnumerator()
inline Enumerator_t4BD1B5394A2E9960562CC364C2CCE4FCBD1AC258 List_1_GetEnumerator_m039302BD172C3288503DB73B6E2B27B8D8BC0321 (List_1_tB56F1028A724D2CE4E84861619D1CF68C68C983D* __this, const RuntimeMethod* method)
{
	return ((  Enumerator_t4BD1B5394A2E9960562CC364C2CCE4FCBD1AC258 (*) (List_1_tB56F1028A724D2CE4E84861619D1CF68C68C983D*, const RuntimeMethod*))List_1_GetEnumerator_m039302BD172C3288503DB73B6E2B27B8D8BC0321_gshared)(__this, method);
}
// System.Void System.Collections.Generic.List`1/Enumerator<UnityEngine.Vector2Int>::Dispose()
inline void Enumerator_Dispose_m2A96F62698864FA1E73292450EBF2019F7104BBC (Enumerator_t4BD1B5394A2E9960562CC364C2CCE4FCBD1AC258* __this, const RuntimeMethod* method)
{
	((  void (*) (Enumerator_t4BD1B5394A2E9960562CC364C2CCE4FCBD1AC258*, const RuntimeMethod*))Enumerator_Dispose_m2A96F62698864FA1E73292450EBF2019F7104BBC_gshared)(__this, method);
}
// T System.Collections.Generic.List`1/Enumerator<UnityEngine.Vector2Int>::get_Current()
inline Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A Enumerator_get_Current_m87245A61572727EBCD1642C4A2BD99B11CE9FA8A_inline (Enumerator_t4BD1B5394A2E9960562CC364C2CCE4FCBD1AC258* __this, const RuntimeMethod* method)
{
	return ((  Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A (*) (Enumerator_t4BD1B5394A2E9960562CC364C2CCE4FCBD1AC258*, const RuntimeMethod*))Enumerator_get_Current_m87245A61572727EBCD1642C4A2BD99B11CE9FA8A_gshared_inline)(__this, method);
}
// System.Boolean System.Collections.Generic.List`1/Enumerator<UnityEngine.Vector2Int>::MoveNext()
inline bool Enumerator_MoveNext_mD6D16710D40F62D081A4973E4D8CA1614D1482B5 (Enumerator_t4BD1B5394A2E9960562CC364C2CCE4FCBD1AC258* __this, const RuntimeMethod* method)
{
	return ((  bool (*) (Enumerator_t4BD1B5394A2E9960562CC364C2CCE4FCBD1AC258*, const RuntimeMethod*))Enumerator_MoveNext_mD6D16710D40F62D081A4973E4D8CA1614D1482B5_gshared)(__this, method);
}
// System.Void CrowdMatch.PixelItem::SetExposed(System.Boolean)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelItem_SetExposed_mBDE7ACCCFA45DD1DE0B8271008CA72B124A07D95 (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, bool ___exposed0, const RuntimeMethod* method) ;
// System.Void UnityEngine.Transform::SetParent(UnityEngine.Transform,System.Boolean)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Transform_SetParent_m9BDD7B7476714B2D7919B10BDC22CE75C0A0A195 (Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* __this, Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* ___parent0, bool ___worldPositionStays1, const RuntimeMethod* method) ;
// System.Boolean UnityEngine.Application::get_isPlaying()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool Application_get_isPlaying_m25B0ABDFEF54F5370CD3F263A813540843D00F34 (const RuntimeMethod* method) ;
// UnityEngine.GameObject UnityEngine.Component::get_gameObject()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* Component_get_gameObject_m57AEFBB14DB39EC476F740BA000E170355DE691B (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* __this, const RuntimeMethod* method) ;
// System.Void UnityEngine.Object::Destroy(UnityEngine.Object)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Object_Destroy_mE97D0A766419A81296E8D4E5C23D01D3FE91ACBB (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C* ___obj0, const RuntimeMethod* method) ;
// System.Void UnityEngine.Object::DestroyImmediate(UnityEngine.Object)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Object_DestroyImmediate_m6336EBC83591A5DB64EC70C92132824C6E258705 (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C* ___obj0, const RuntimeMethod* method) ;
// System.Void UnityEngine.Debug::LogError(System.Object)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Debug_LogError_mB00B2B4468EF3CAF041B038D840820FB84C924B2 (RuntimeObject* ___message0, const RuntimeMethod* method) ;
// System.Int32 UnityEngine.Transform::get_childCount()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t Transform_get_childCount_mE9C29C702AB662CC540CA053EDE48BDAFA35B4B0 (Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* __this, const RuntimeMethod* method) ;
// System.String System.Int32::ToString()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* Int32_ToString_m030E01C24E294D6762FB0B6F37CB541581F55CA5 (int32_t* __this, const RuntimeMethod* method) ;
// System.String System.String::Concat(System.String,System.String)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* String_Concat_m9E3155FB84015C823606188F53B47CB44C444991 (String_t* ___str00, String_t* ___str11, const RuntimeMethod* method) ;
// System.Void UnityEngine.GameObject::.ctor(System.String)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void GameObject__ctor_m37D512B05D292F954792225E6C6EEE95293A9B88 (GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* __this, String_t* ___name0, const RuntimeMethod* method) ;
// UnityEngine.Transform UnityEngine.GameObject::get_transform()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* GameObject_get_transform_m0BC10ADFA1632166AE5544BDF9038A2650C2AE56 (GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* __this, const RuntimeMethod* method) ;
// UnityEngine.Vector3 UnityEngine.Vector3::get_zero()
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_get_zero_m0C1249C3F25B1C70EAD3CC8B31259975A457AE39_inline (const RuntimeMethod* method) ;
// T UnityEngine.GameObject::AddComponent<CrowdMatch.WallItem>()
inline WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* GameObject_AddComponent_TisWallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0_mF275F42400D186788F6E0B363E9F0D081AB0FD8A (GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* __this, const RuntimeMethod* method)
{
	return ((  WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* (*) (GameObject_t76FEDD663AB33C991A9C9A23129337651094216F*, const RuntimeMethod*))GameObject_AddComponent_TisRuntimeObject_m69B93700FACCF372F5753371C6E8FB780800B824_gshared)(__this, method);
}
// System.Void System.Collections.Generic.List`1<UnityEngine.Vector2>::.ctor(System.Collections.Generic.IEnumerable`1<T>)
inline void List_1__ctor_m105596C2159C46B75E96D26ACEC0A5C1C1F5C5EC (List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B* __this, RuntimeObject* ___collection0, const RuntimeMethod* method)
{
	((  void (*) (List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B*, RuntimeObject*, const RuntimeMethod*))List_1__ctor_m105596C2159C46B75E96D26ACEC0A5C1C1F5C5EC_gshared)(__this, ___collection0, method);
}
// T UnityEngine.Object::Instantiate<UnityEngine.GameObject>(T)
inline GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* Object_Instantiate_TisGameObject_t76FEDD663AB33C991A9C9A23129337651094216F_m10D87C6E0708CA912BBB02555BF7D0FBC5D7A2B3 (GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* ___original0, const RuntimeMethod* method)
{
	return ((  GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* (*) (GameObject_t76FEDD663AB33C991A9C9A23129337651094216F*, const RuntimeMethod*))Object_Instantiate_TisRuntimeObject_m90A1E6C4C2B445D2E848DB75C772D1B95AAC046A_gshared)(___original0, method);
}
// System.String System.String::Concat(System.String,System.String,System.String,System.String)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* String_Concat_m093934F71A9B351911EE46311674ED463B180006 (String_t* ___str00, String_t* ___str11, String_t* ___str22, String_t* ___str33, const RuntimeMethod* method) ;
// System.Void UnityEngine.Object::set_name(System.String)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Object_set_name_mC79E6DC8FFD72479C90F0C4CC7F42A0FEAF5AE47 (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C* __this, String_t* ___value0, const RuntimeMethod* method) ;
// UnityEngine.Vector3 UnityEngine.Vector3::get_one()
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_get_one_mC9B289F1E15C42C597180C9FE6FB492495B51D02_inline (const RuntimeMethod* method) ;
// UnityEngine.Vector3 UnityEngine.Vector3::op_Multiply(UnityEngine.Vector3,System.Single)
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_op_Multiply_m87BA7C578F96C8E49BB07088DAAC4649F83B0353_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___a0, float ___d1, const RuntimeMethod* method) ;
// System.Void UnityEngine.Transform::set_localScale(UnityEngine.Vector3)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Transform_set_localScale_mBA79E811BAF6C47B80FF76414C12B47B3CD03633 (Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* __this, Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___value0, const RuntimeMethod* method) ;
// T UnityEngine.GameObject::GetComponent<CrowdMatch.PixelItem>()
inline PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* GameObject_GetComponent_TisPixelItem_t863890C77945A8C08435BF2F8859A47A413C794E_m7C12F9FAB885712E9777CD52832D09C1C0C4989B (GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* __this, const RuntimeMethod* method)
{
	return ((  PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* (*) (GameObject_t76FEDD663AB33C991A9C9A23129337651094216F*, const RuntimeMethod*))GameObject_GetComponent_TisRuntimeObject_m6EAED4AA356F0F48288F67899E5958792395563B_gshared)(__this, method);
}
// System.String UnityEngine.Object::get_name()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* Object_get_name_mAC2F6B897CF1303BA4249B4CB55271AFACBB6392 (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C* __this, const RuntimeMethod* method) ;
// System.String System.String::Concat(System.String,System.String,System.String)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* String_Concat_m8855A6DE10F84DA7F4EC113CADDB59873A25573B (String_t* ___str00, String_t* ___str11, String_t* ___str22, const RuntimeMethod* method) ;
// System.Void CrowdMatch.PixelItem::ApplyMaterial(CrowdMatch.ColorConfig)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelItem_ApplyMaterial_m0CE545B161D5D3352A6C908088C9AC241381364F (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, ColorConfig_tCFE08A98D99EE62711B453124E88292D1545172C* ___config0, const RuntimeMethod* method) ;
// System.Void System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(System.Array,System.RuntimeFieldHandle)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RuntimeHelpers_InitializeArray_m751372AA3F24FBF6DA9B9D687CBFA2DE436CAB9B (RuntimeArray* ___array0, RuntimeFieldHandle_t6E4C45B6D2EA12FC99185805A7E77527899B25C5 ___fldHandle1, const RuntimeMethod* method) ;
// System.Void CrowdMatch.PixelItem::BindClickListener()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelItem_BindClickListener_mB988349ABA6193468BE141196FE282ACB5CCDFB6 (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, const RuntimeMethod* method) ;
// T UnityEngine.Component::GetComponentInChildren<CrowdMatch.PixelClickListener>(System.Boolean)
inline PixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524* Component_GetComponentInChildren_TisPixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524_m373C622F45C151B1A0749FD577AFDC09F0488041 (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* __this, bool ___includeInactive0, const RuntimeMethod* method)
{
	return ((  PixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524* (*) (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3*, bool, const RuntimeMethod*))Component_GetComponentInChildren_TisRuntimeObject_m831BC1785A9E9CB99F8D66BDFCF6D606622B5ADB_gshared)(__this, ___includeInactive0, method);
}
// System.Void CrowdMatch.PixelClickListener::SetClickable(System.Boolean)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelClickListener_SetClickable_mC408165A086B268E7D4D348CC90640DD8DCF5EC0 (PixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524* __this, bool ___clickable0, const RuntimeMethod* method) ;
// System.Void CrowdMatch.PixelItem::ApplyWalking()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelItem_ApplyWalking_m96050E3E8F34DBE985E12C82E388E6F8481A1FED (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, const RuntimeMethod* method) ;
// System.Void CrowdMatch.PixelItem::ApplyIdle()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelItem_ApplyIdle_m4164B244DBE984A03C3739C59AE5F86C72077BE9 (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, const RuntimeMethod* method) ;
// System.Void UnityEngine.Behaviour::set_enabled(System.Boolean)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Behaviour_set_enabled_mF1DCFE60EB09E0529FE9476CA804A3AA2D72B16A (Behaviour_t01970CFBBA658497AE30F311C447DB0440BAB7FA* __this, bool ___value0, const RuntimeMethod* method) ;
// System.Void UnityEngine.Animator::SetBool(System.String,System.Boolean)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Animator_SetBool_m6F8D4FAF0770CD4EC1F54406249785DE7391E42B (Animator_t8A52E42AE54F76681838FE9E632683EF3952E883* __this, String_t* ___name0, bool ___value1, const RuntimeMethod* method) ;
// System.Int32 DG.Tweening.ShortcutExtensions::DOKill(UnityEngine.Component,System.Boolean)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t ShortcutExtensions_DOKill_m3F197E779AB6CA95FF3C4C2DD547B4B493E42D46 (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* ___target0, bool ___complete1, const RuntimeMethod* method) ;
// UnityEngine.Quaternion UnityEngine.Quaternion::get_identity()
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 Quaternion_get_identity_m7E701AE095ED10FD5EA0B50ABCFDE2EEFF2173A5_inline (const RuntimeMethod* method) ;
// System.Void UnityEngine.Transform::set_localRotation(UnityEngine.Quaternion)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Transform_set_localRotation_mAB4A011D134BA58AB780BECC0025CA65F16185FA (Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* __this, Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 ___value0, const RuntimeMethod* method) ;
// System.Void UnityEngine.Animator::set_applyRootMotion(System.Boolean)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Animator_set_applyRootMotion_mA0953B6AEE43D4AF0837365E7BFF60FCC74B0F98 (Animator_t8A52E42AE54F76681838FE9E632683EF3952E883* __this, bool ___value0, const RuntimeMethod* method) ;
// DG.Tweening.Core.TweenerCore`3<UnityEngine.Vector3,UnityEngine.Vector3,DG.Tweening.Plugins.Options.VectorOptions> DG.Tweening.ShortcutExtensions::DOLocalMove(UnityEngine.Transform,UnityEngine.Vector3,System.Single,System.Boolean)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR TweenerCore_3_tCD82DFC45FB71C681FA8659EA63A7D7D16BFFE77* ShortcutExtensions_DOLocalMove_m22F3EB581DADB5A3FC59B69F7F6F05A86F8E8348 (Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* ___target0, Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___endValue1, float ___duration2, bool ___snapping3, const RuntimeMethod* method) ;
// DG.Tweening.Core.TweenerCore`3<UnityEngine.Quaternion,UnityEngine.Vector3,DG.Tweening.Plugins.Options.QuaternionOptions> DG.Tweening.ShortcutExtensions::DOLocalRotate(UnityEngine.Transform,UnityEngine.Vector3,System.Single,DG.Tweening.RotateMode)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR TweenerCore_3_t392C54729BB024F5802F8E205C583653C4E886E3* ShortcutExtensions_DOLocalRotate_m6EB8F37963023C6B157C60013B98D2B612816DA4 (Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* ___target0, Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___endValue1, float ___duration2, int32_t ___mode3, const RuntimeMethod* method) ;
// System.Void DG.Tweening.TweenCallback::.ctor(System.Object,System.IntPtr)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void TweenCallback__ctor_m68CC9304423CBDE43001F9B1413B5DAAF70DB621 (TweenCallback_t7C8B8A38E7B30905FF1B83C943256EF23617BB24* __this, RuntimeObject* ___object0, intptr_t ___method1, const RuntimeMethod* method) ;
// T DG.Tweening.TweenSettingsExtensions::OnComplete<DG.Tweening.Core.TweenerCore`3<UnityEngine.Quaternion,UnityEngine.Vector3,DG.Tweening.Plugins.Options.QuaternionOptions>>(T,DG.Tweening.TweenCallback)
inline TweenerCore_3_t392C54729BB024F5802F8E205C583653C4E886E3* TweenSettingsExtensions_OnComplete_TisTweenerCore_3_t392C54729BB024F5802F8E205C583653C4E886E3_m8BE213B05FF94E9AE889B180DB8B3DE6EC4EB1E1 (TweenerCore_3_t392C54729BB024F5802F8E205C583653C4E886E3* ___t0, TweenCallback_t7C8B8A38E7B30905FF1B83C943256EF23617BB24* ___action1, const RuntimeMethod* method)
{
	return ((  TweenerCore_3_t392C54729BB024F5802F8E205C583653C4E886E3* (*) (TweenerCore_3_t392C54729BB024F5802F8E205C583653C4E886E3*, TweenCallback_t7C8B8A38E7B30905FF1B83C943256EF23617BB24*, const RuntimeMethod*))TweenSettingsExtensions_OnComplete_TisRuntimeObject_mC014D07E92193DA79B257C4508B6DF208FE502A6_gshared)(___t0, ___action1, method);
}
// System.Void UnityEngine.MonoBehaviour::StopCoroutine(UnityEngine.Coroutine)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void MonoBehaviour_StopCoroutine_mB0FC91BE84203BD8E360B3FBAE5B958B4C5ED22A (MonoBehaviour_t532A11E69716D348D8AA7F854AFCBFCB8AD17F71* __this, Coroutine_t85EA685566A254C23F3FD77AB5BDFFFF8799596B* ___routine0, const RuntimeMethod* method) ;
// System.Collections.IEnumerator CrowdMatch.PixelItem::MoveExposeTargetToY(System.Single,System.Single)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* PixelItem_MoveExposeTargetToY_m6C859784BAA8F63BB768D507FB0F56E78C7A5AF1 (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, float ___targetY0, float ___duration1, const RuntimeMethod* method) ;
// UnityEngine.Coroutine UnityEngine.MonoBehaviour::StartCoroutine(System.Collections.IEnumerator)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Coroutine_t85EA685566A254C23F3FD77AB5BDFFFF8799596B* MonoBehaviour_StartCoroutine_m4CAFF732AA28CD3BDC5363B44A863575530EC812 (MonoBehaviour_t532A11E69716D348D8AA7F854AFCBFCB8AD17F71* __this, RuntimeObject* ___routine0, const RuntimeMethod* method) ;
// UnityEngine.Material CrowdMatch.ColorConfig::GetMaterial(System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* ColorConfig_GetMaterial_mA4E421D41F9F046F6E209813332A86C790103589 (ColorConfig_tCFE08A98D99EE62711B453124E88292D1545172C* __this, int32_t ___colorId0, const RuntimeMethod* method) ;
// System.Collections.Generic.List`1/Enumerator<T> System.Collections.Generic.List`1<UnityEngine.Renderer>::GetEnumerator()
inline Enumerator_t4B5D90D1324DE6E043169A1E8DCD75512559AFA7 List_1_GetEnumerator_mC86A1EF9E784B7E7B5C00025383C6381B831F88C (List_1_tD435DCC2A88E36DFC551EA5392CE0182F0C50E93* __this, const RuntimeMethod* method)
{
	return ((  Enumerator_t4B5D90D1324DE6E043169A1E8DCD75512559AFA7 (*) (List_1_tD435DCC2A88E36DFC551EA5392CE0182F0C50E93*, const RuntimeMethod*))List_1_GetEnumerator_mD8294A7FA2BEB1929487127D476F8EC1CDC23BFC_gshared)(__this, method);
}
// System.Void System.Collections.Generic.List`1/Enumerator<UnityEngine.Renderer>::Dispose()
inline void Enumerator_Dispose_m39794B37E9AE88ED22C03824DE8D637C0DADBAF0 (Enumerator_t4B5D90D1324DE6E043169A1E8DCD75512559AFA7* __this, const RuntimeMethod* method)
{
	((  void (*) (Enumerator_t4B5D90D1324DE6E043169A1E8DCD75512559AFA7*, const RuntimeMethod*))Enumerator_Dispose_mD9DC3E3C3697830A4823047AB29A77DBBB5ED419_gshared)(__this, method);
}
// T System.Collections.Generic.List`1/Enumerator<UnityEngine.Renderer>::get_Current()
inline Renderer_t320575F223BCB177A982E5DDB5DB19FAA89E7FBF* Enumerator_get_Current_mF219FEB0F2097ED593A4E6E2283167335AFBA4B6_inline (Enumerator_t4B5D90D1324DE6E043169A1E8DCD75512559AFA7* __this, const RuntimeMethod* method)
{
	return ((  Renderer_t320575F223BCB177A982E5DDB5DB19FAA89E7FBF* (*) (Enumerator_t4B5D90D1324DE6E043169A1E8DCD75512559AFA7*, const RuntimeMethod*))Enumerator_get_Current_m6330F15D18EE4F547C05DF9BF83C5EB710376027_gshared_inline)(__this, method);
}
// System.Void UnityEngine.Renderer::set_sharedMaterial(UnityEngine.Material)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Renderer_set_sharedMaterial_m5E842F9A06CFB7B77656EB319881CB4B3E8E4288 (Renderer_t320575F223BCB177A982E5DDB5DB19FAA89E7FBF* __this, Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* ___value0, const RuntimeMethod* method) ;
// System.Boolean System.Collections.Generic.List`1/Enumerator<UnityEngine.Renderer>::MoveNext()
inline bool Enumerator_MoveNext_m36545FD9B7C5DA66EAD80FD8813D279003BA8749 (Enumerator_t4B5D90D1324DE6E043169A1E8DCD75512559AFA7* __this, const RuntimeMethod* method)
{
	return ((  bool (*) (Enumerator_t4B5D90D1324DE6E043169A1E8DCD75512559AFA7*, const RuntimeMethod*))Enumerator_MoveNext_mE921CC8F29FBBDE7CC3209A0ED0D921D58D00BCB_gshared)(__this, method);
}
// System.Boolean CrowdMatch.PixelItem::get_IsExposed()
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR bool PixelItem_get_IsExposed_mC91D7897C85B411BACD2B7CA57A5D9473985125B_inline (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, const RuntimeMethod* method) ;
// System.Void CrowdMatch.PixelItem::set_IsExposed(System.Boolean)
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void PixelItem_set_IsExposed_m7C2512B8A1E4DEB1D563791BDFB493BF5C98DBCB_inline (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, bool ___value0, const RuntimeMethod* method) ;
// System.Void CrowdMatch.PixelItem/<MoveExposeTargetToY>d__35::.ctor(System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void U3CMoveExposeTargetToYU3Ed__35__ctor_mF04ADD1A971AB52742A40EAF409EAD8FA5CE22F2 (U3CMoveExposeTargetToYU3Ed__35_tAD80531ED95A191AB90EE89508DFC17246399033* __this, int32_t ___U3CU3E1__state0, const RuntimeMethod* method) ;
// System.Void System.Collections.Generic.List`1<UnityEngine.Renderer>::.ctor()
inline void List_1__ctor_m803E10F7A50EB22BF82C0C1AB251D5407B4496DE (List_1_tD435DCC2A88E36DFC551EA5392CE0182F0C50E93* __this, const RuntimeMethod* method)
{
	((  void (*) (List_1_tD435DCC2A88E36DFC551EA5392CE0182F0C50E93*, const RuntimeMethod*))List_1__ctor_m7F078BB342729BDF11327FD89D7872265328F690_gshared)(__this, method);
}
// System.Single UnityEngine.Mathf::Max(System.Single,System.Single)
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float Mathf_Max_mF5379E63D2BBAC76D090748695D833934F8AD051_inline (float ___a0, float ___b1, const RuntimeMethod* method) ;
// System.Void System.Collections.Generic.List`1<System.ValueTuple`3<System.Int32,System.Int32,System.Int32>>::.ctor()
inline void List_1__ctor_m7734609AB0CE32B017F3FBC89E3A0D35323A5015 (List_1_t264DA9C475A0E3CF57148457209560CF2E2B6F27* __this, const RuntimeMethod* method)
{
	((  void (*) (List_1_t264DA9C475A0E3CF57148457209560CF2E2B6F27*, const RuntimeMethod*))List_1__ctor_m7734609AB0CE32B017F3FBC89E3A0D35323A5015_gshared)(__this, method);
}
// TResult System.Func`3<System.Int32,System.Int32,System.Boolean>::Invoke(T1,T2)
inline bool Func_3_Invoke_m5C4CCADFF1AE4540F252182089A9BF3CBE7BAFE6_inline (Func_3_tE8F85DA3CAC4998201E5C56356280AFAB7185B69* __this, int32_t ___arg10, int32_t ___arg21, const RuntimeMethod* method)
{
	return ((  bool (*) (Func_3_tE8F85DA3CAC4998201E5C56356280AFAB7185B69*, int32_t, int32_t, const RuntimeMethod*))Func_3_Invoke_m5C4CCADFF1AE4540F252182089A9BF3CBE7BAFE6_gshared_inline)(__this, ___arg10, ___arg21, method);
}
// System.Int32 UnityEngine.Mathf::Clamp(System.Int32,System.Int32,System.Int32)
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t Mathf_Clamp_m4DC36EEFDBE5F07C16249DA568023C5ECCFF0E7B_inline (int32_t ___value0, int32_t ___min1, int32_t ___max2, const RuntimeMethod* method) ;
// UnityEngine.Color UnityEngine.Texture2D::GetPixel(System.Int32,System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Color_tD001788D726C3A7F1379BEED0260B9591F440C1F Texture2D_GetPixel_m69A17FE5CC220F438C7421DCB50A9E22AAB4A415 (Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* __this, int32_t ___x0, int32_t ___y1, const RuntimeMethod* method) ;
// System.Int32 CrowdMatch.GridColorMatcher::FindClosestColorIndex(UnityEngine.Color,UnityEngine.Color[])
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t GridColorMatcher_FindClosestColorIndex_mC714B659FAA530B6286BC986B6B3FD98645B5729 (Color_tD001788D726C3A7F1379BEED0260B9591F440C1F ___target0, ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* ___palette1, const RuntimeMethod* method) ;
// System.Void System.ValueTuple`3<System.Int32,System.Int32,System.Int32>::.ctor(T1,T2,T3)
inline void ValueTuple_3__ctor_m0D7E698F23721325245996D0B8DED1C102559F60 (ValueTuple_3_tBFE24970AFB182B3C77F4C747E3F1E7A7B4EBC57* __this, int32_t ___item10, int32_t ___item21, int32_t ___item32, const RuntimeMethod* method)
{
	((  void (*) (ValueTuple_3_tBFE24970AFB182B3C77F4C747E3F1E7A7B4EBC57*, int32_t, int32_t, int32_t, const RuntimeMethod*))ValueTuple_3__ctor_m0D7E698F23721325245996D0B8DED1C102559F60_gshared)(__this, ___item10, ___item21, ___item32, method);
}
// System.Void System.Collections.Generic.List`1<System.ValueTuple`3<System.Int32,System.Int32,System.Int32>>::Add(T)
inline void List_1_Add_m716377944B1E88CBB1B269AA9CF38C525A41D367_inline (List_1_t264DA9C475A0E3CF57148457209560CF2E2B6F27* __this, ValueTuple_3_tBFE24970AFB182B3C77F4C747E3F1E7A7B4EBC57 ___item0, const RuntimeMethod* method)
{
	((  void (*) (List_1_t264DA9C475A0E3CF57148457209560CF2E2B6F27*, ValueTuple_3_tBFE24970AFB182B3C77F4C747E3F1E7A7B4EBC57, const RuntimeMethod*))List_1_Add_m716377944B1E88CBB1B269AA9CF38C525A41D367_gshared_inline)(__this, ___item0, method);
}
// System.Void UnityEngine.Texture2D::.ctor(System.Int32,System.Int32,UnityEngine.TextureFormat,System.Boolean)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Texture2D__ctor_mECF60A9EC0638EC353C02C8E99B6B465D23BE917 (Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* __this, int32_t ___width0, int32_t ___height1, int32_t ___textureFormat2, bool ___mipChain3, const RuntimeMethod* method) ;
// System.Boolean System.Nullable`1<UnityEngine.Color>::get_HasValue()
inline bool Nullable_1_get_HasValue_mFAF0B4EEA878E596C80258FE3BDA57CEF40C8D7F_inline (Nullable_1_tEE83D90B507D40B6C58B5EEF5B9D44D377B44F11* __this, const RuntimeMethod* method)
{
	return ((  bool (*) (Nullable_1_tEE83D90B507D40B6C58B5EEF5B9D44D377B44F11*, const RuntimeMethod*))Nullable_1_get_HasValue_mFAF0B4EEA878E596C80258FE3BDA57CEF40C8D7F_gshared_inline)(__this, method);
}
// UnityEngine.Color UnityEngine.Color::get_white()
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Color_tD001788D726C3A7F1379BEED0260B9591F440C1F Color_get_white_m068F5AF879B0FCA584E3693F762EA41BB65532C6_inline (const RuntimeMethod* method) ;
// T System.Nullable`1<UnityEngine.Color>::GetValueOrDefault()
inline Color_tD001788D726C3A7F1379BEED0260B9591F440C1F Nullable_1_GetValueOrDefault_m9A7869C021F041D45F2A851F70F97F8114AC99E4_inline (Nullable_1_tEE83D90B507D40B6C58B5EEF5B9D44D377B44F11* __this, const RuntimeMethod* method)
{
	return ((  Color_tD001788D726C3A7F1379BEED0260B9591F440C1F (*) (Nullable_1_tEE83D90B507D40B6C58B5EEF5B9D44D377B44F11*, const RuntimeMethod*))Nullable_1_GetValueOrDefault_m9A7869C021F041D45F2A851F70F97F8114AC99E4_gshared_inline)(__this, method);
}
// TResult System.Func`3<System.Int32,System.Int32,System.Nullable`1<UnityEngine.Color>>::Invoke(T1,T2)
inline Nullable_1_tEE83D90B507D40B6C58B5EEF5B9D44D377B44F11 Func_3_Invoke_mADC4326AE0426011BAE02568945B03277B225B79_inline (Func_3_t171A20B65D1078C91C9A875EF6CBB2716C7E322D* __this, int32_t ___arg10, int32_t ___arg21, const RuntimeMethod* method)
{
	return ((  Nullable_1_tEE83D90B507D40B6C58B5EEF5B9D44D377B44F11 (*) (Func_3_t171A20B65D1078C91C9A875EF6CBB2716C7E322D*, int32_t, int32_t, const RuntimeMethod*))Func_3_Invoke_mADC4326AE0426011BAE02568945B03277B225B79_gshared_inline)(__this, ___arg10, ___arg21, method);
}
// T System.Nullable`1<UnityEngine.Color>::get_Value()
inline Color_tD001788D726C3A7F1379BEED0260B9591F440C1F Nullable_1_get_Value_m3FC15B40E747AA6A9F2AB10A00C46ABE84393637 (Nullable_1_tEE83D90B507D40B6C58B5EEF5B9D44D377B44F11* __this, const RuntimeMethod* method)
{
	return ((  Color_tD001788D726C3A7F1379BEED0260B9591F440C1F (*) (Nullable_1_tEE83D90B507D40B6C58B5EEF5B9D44D377B44F11*, const RuntimeMethod*))Nullable_1_get_Value_m3FC15B40E747AA6A9F2AB10A00C46ABE84393637_gshared)(__this, method);
}
// System.Void UnityEngine.Texture2D::SetPixels(UnityEngine.Color[])
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Texture2D_SetPixels_mAE0CDFA15FA96F840D7FFADC31405D8AF20D9073 (Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* __this, ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* ___colors0, const RuntimeMethod* method) ;
// System.Void UnityEngine.Texture2D::Apply()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Texture2D_Apply_mA014182C9EE0BBF6EEE3B286854F29E50EB972DC (Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* __this, const RuntimeMethod* method) ;
// T UnityEngine.Component::GetComponentInParent<CrowdMatch.PixelGroup>()
inline PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* Component_GetComponentInParent_TisPixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80_mD60D9A2DE017B170590FDF6B2CB4CB66A737CD4F (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3* __this, const RuntimeMethod* method)
{
	return ((  PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* (*) (Component_t39FBE53E5EFCF4409111FB22C15FF73717632EC3*, const RuntimeMethod*))Component_GetComponentInParent_TisRuntimeObject_m6746D6BB99912B1B509746C993906492F86CD119_gshared)(__this, method);
}
// System.Int32 UnityEngine.Mathf::RoundToInt(System.Single)
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t Mathf_RoundToInt_m60F8B66CF27F1FA75AA219342BD184B75771EB4B_inline (float ___f0, const RuntimeMethod* method) ;
// T System.Collections.Generic.List`1<UnityEngine.Vector2>::get_Item(System.Int32)
inline Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 List_1_get_Item_m1F8E226CAD72B83C5E75BB66B43025247806B543 (List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B* __this, int32_t ___index0, const RuntimeMethod* method)
{
	return ((  Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 (*) (List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B*, int32_t, const RuntimeMethod*))List_1_get_Item_m1F8E226CAD72B83C5E75BB66B43025247806B543_gshared)(__this, ___index0, method);
}
// System.String UnityEngine.Vector2::ToString()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* Vector2_ToString_mB47B29ECB21FA3A4ACEABEFA18077A5A6BBCCB27 (Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7* __this, const RuntimeMethod* method) ;
// System.String System.String::Concat(System.String[])
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* String_Concat_m647EBF831F54B6DF7D5AFA5FD012CF4EE7571B6A (StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* ___values0, const RuntimeMethod* method) ;
// System.Int32 System.Collections.Generic.List`1<UnityEngine.Vector2>::get_Count()
inline int32_t List_1_get_Count_mF1C0C56457C655BDFFC6EE5B46FAD8BAEC1F588B_inline (List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B* __this, const RuntimeMethod* method)
{
	return ((  int32_t (*) (List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B*, const RuntimeMethod*))List_1_get_Count_mF1C0C56457C655BDFFC6EE5B46FAD8BAEC1F588B_gshared_inline)(__this, method);
}
// UnityEngine.Vector2Int CrowdMatch.WallItem::ToCell(UnityEngine.Vector2)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A WallItem_ToCell_mFA83E7FB1FBAB3694E1F9640EC1E3988748B61C6 (Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 ___p0, const RuntimeMethod* method) ;
// System.Int32 UnityEngine.Mathf::Abs(System.Int32)
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t Mathf_Abs_mD945EDDEA0D62D21BFDBAB7B1C0F18DFF1CEC905_inline (int32_t ___value0, const RuntimeMethod* method) ;
// System.Int32 System.Math::Sign(System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t Math_Sign_m1E922CDC910F3DAFCC8DB60D4C33BA00CF1AB5D4 (int32_t ___value0, const RuntimeMethod* method) ;
// System.Boolean System.Collections.Generic.HashSet`1<UnityEngine.Vector2Int>::Add(T)
inline bool HashSet_1_Add_m517B9238E386530A854B4286891358CC8327A7B6 (HashSet_1_t55B946E0B889BDE32B3004D0822A6C5017AFA406* __this, Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A ___item0, const RuntimeMethod* method)
{
	return ((  bool (*) (HashSet_1_t55B946E0B889BDE32B3004D0822A6C5017AFA406*, Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A, const RuntimeMethod*))HashSet_1_Add_m517B9238E386530A854B4286891358CC8327A7B6_gshared)(__this, ___item0, method);
}
// System.Void CrowdMatch.WallItem::EnumerateSegment(UnityEngine.Vector2,UnityEngine.Vector2,System.Collections.Generic.HashSet`1<UnityEngine.Vector2Int>)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void WallItem_EnumerateSegment_mFBA3CBBEBD03941811D1207EE8559B01C5C2D5BC (Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 ___a0, Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 ___b1, HashSet_1_t55B946E0B889BDE32B3004D0822A6C5017AFA406* ___set2, const RuntimeMethod* method) ;
// System.Void System.Collections.Generic.HashSet`1<UnityEngine.Vector2Int>::.ctor()
inline void HashSet_1__ctor_m0E27874668BB3B3160062D69799276CFEF8072AB (HashSet_1_t55B946E0B889BDE32B3004D0822A6C5017AFA406* __this, const RuntimeMethod* method)
{
	((  void (*) (HashSet_1_t55B946E0B889BDE32B3004D0822A6C5017AFA406*, const RuntimeMethod*))HashSet_1__ctor_m0E27874668BB3B3160062D69799276CFEF8072AB_gshared)(__this, method);
}
// System.Void CrowdMatch.WallItem::CollectOccupiedCells(System.Collections.Generic.IReadOnlyList`1<UnityEngine.Vector2>,System.Collections.Generic.HashSet`1<UnityEngine.Vector2Int>)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void WallItem_CollectOccupiedCells_m726DF1E889297C025677C298A7FD3128CE48BF70 (RuntimeObject* ___points0, HashSet_1_t55B946E0B889BDE32B3004D0822A6C5017AFA406* ___set1, const RuntimeMethod* method) ;
// CrowdMatch.PixelGroup CrowdMatch.WallItem::get_Group()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* WallItem_get_Group_m467F5ED6CCE4DFEFEBF4F86532BE7ED2CD614729 (WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* __this, const RuntimeMethod* method) ;
// UnityEngine.Vector3 CrowdMatch.PixelGroup::GetWorldPosition(System.Int32,System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 PixelGroup_GetWorldPosition_m53DF201A615D1467AB8512D30C752A3826FBE608 (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, int32_t ___col0, int32_t ___row1, const RuntimeMethod* method) ;
// System.Void UnityEngine.Gizmos::set_color(UnityEngine.Color)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Gizmos_set_color_m53927A2741937484180B20B55F7F20F8F60C5797 (Color_tD001788D726C3A7F1379BEED0260B9591F440C1F ___value0, const RuntimeMethod* method) ;
// UnityEngine.Vector3 CrowdMatch.WallItem::CellWorld(UnityEngine.Vector2)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 WallItem_CellWorld_mD3D389CFFC4517B5825563C76508F9245868E006 (WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* __this, Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 ___p0, const RuntimeMethod* method) ;
// UnityEngine.Vector3 UnityEngine.Vector3::get_up()
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_get_up_m128AF3FDC820BF59D5DE86D973E7DE3F20C3AEBA_inline (const RuntimeMethod* method) ;
// UnityEngine.Vector3 UnityEngine.Vector3::op_Addition(UnityEngine.Vector3,UnityEngine.Vector3)
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_op_Addition_m78C0EC70CB66E8DCAC225743D82B268DAEE92067_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___a0, Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___b1, const RuntimeMethod* method) ;
// System.Void UnityEngine.Gizmos::DrawLine(UnityEngine.Vector3,UnityEngine.Vector3)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Gizmos_DrawLine_mB139054F55D615637A39A3127AADB16043387F8A (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___from0, Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___to1, const RuntimeMethod* method) ;
// System.Void UnityEngine.Gizmos::DrawCube(UnityEngine.Vector3,UnityEngine.Vector3)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Gizmos_DrawCube_m4417EAEA479EF4AD52445810D840BA8FCBC6EF3F (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___center0, Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___size1, const RuntimeMethod* method) ;
// UnityEngine.Color UnityEngine.Gizmos::get_color()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Color_tD001788D726C3A7F1379BEED0260B9591F440C1F Gizmos_get_color_mF7A6194876F0DB8D2629715134BAAD3765849A3B (const RuntimeMethod* method) ;
// System.Void System.Collections.Generic.List`1<UnityEngine.Vector2>::.ctor()
inline void List_1__ctor_m88C4BD8AC607DB3585552068F4DC437406358D5F (List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B* __this, const RuntimeMethod* method)
{
	((  void (*) (List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B*, const RuntimeMethod*))List_1__ctor_m88C4BD8AC607DB3585552068F4DC437406358D5F_gshared)(__this, method);
}
// System.Void UnityEngine.Color::.ctor(System.Single,System.Single,System.Single,System.Single)
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Color__ctor_m3786F0D6E510D9CFA544523A955870BD2A514C8C_inline (Color_tD001788D726C3A7F1379BEED0260B9591F440C1F* __this, float ___r0, float ___g1, float ___b2, float ___a3, const RuntimeMethod* method) ;
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
// System.Void CrowdMatch.GameController/<GetNeighbors>d__43::.ctor(System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void U3CGetNeighborsU3Ed__43__ctor_m715F3C12C7FEB137BF9A1396437FD0D1D2120204 (U3CGetNeighborsU3Ed__43_t7EE77D676072170DE792051688D0316271D3643C* __this, int32_t ___U3CU3E1__state0, const RuntimeMethod* method) 
{
	{
		Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2(__this, NULL);
		int32_t L_0 = ___U3CU3E1__state0;
		__this->___U3CU3E1__state_0 = L_0;
		int32_t L_1;
		L_1 = Environment_get_CurrentManagedThreadId_m66483AADCCC13272EBDCD94D31D2E52603C24BDF(NULL);
		__this->___U3CU3El__initialThreadId_2 = L_1;
		return;
	}
}
// System.Void CrowdMatch.GameController/<GetNeighbors>d__43::System.IDisposable.Dispose()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void U3CGetNeighborsU3Ed__43_System_IDisposable_Dispose_mE5C4874A2B7FEF3F05948F15A3A7458781A1E779 (U3CGetNeighborsU3Ed__43_t7EE77D676072170DE792051688D0316271D3643C* __this, const RuntimeMethod* method) 
{
	{
		return;
	}
}
// System.Boolean CrowdMatch.GameController/<GetNeighbors>d__43::MoveNext()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool U3CGetNeighborsU3Ed__43_MoveNext_m83AE00DEA9DDF159B592E29589CCA569012F6997 (U3CGetNeighborsU3Ed__43_t7EE77D676072170DE792051688D0316271D3643C* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	GameController_t9B394943D9DA551993B8515B21F692D9B0E00853* V_1 = NULL;
	PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* V_2 = NULL;
	int32_t V_3 = 0;
	{
		int32_t L_0 = __this->___U3CU3E1__state_0;
		V_0 = L_0;
		GameController_t9B394943D9DA551993B8515B21F692D9B0E00853* L_1 = __this->___U3CU3E4__this_3;
		V_1 = L_1;
		int32_t L_2 = V_0;
		if (!L_2)
		{
			goto IL_001a;
		}
	}
	{
		int32_t L_3 = V_0;
		if ((((int32_t)L_3) == ((int32_t)1)))
		{
			goto IL_00a9;
		}
	}
	{
		return (bool)0;
	}

IL_001a:
	{
		__this->___U3CU3E1__state_0 = (-1);
		// int[] dx = { 1, -1, 0, 0 };
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_4 = (Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C*)(Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C*)SZArrayNew(Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C_il2cpp_TypeInfo_var, (uint32_t)4);
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_5 = L_4;
		NullCheck(L_5);
		(L_5)->SetAt(static_cast<il2cpp_array_size_t>(0), (int32_t)1);
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_6 = L_5;
		NullCheck(L_6);
		(L_6)->SetAt(static_cast<il2cpp_array_size_t>(1), (int32_t)(-1));
		__this->___U3CdxU3E5__2_6 = L_6;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___U3CdxU3E5__2_6), (void*)L_6);
		// int[] dz = { 0, 0, 1, -1 };
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_7 = (Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C*)(Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C*)SZArrayNew(Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C_il2cpp_TypeInfo_var, (uint32_t)4);
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_8 = L_7;
		NullCheck(L_8);
		(L_8)->SetAt(static_cast<il2cpp_array_size_t>(2), (int32_t)1);
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_9 = L_8;
		NullCheck(L_9);
		(L_9)->SetAt(static_cast<il2cpp_array_size_t>(3), (int32_t)(-1));
		__this->___U3CdzU3E5__3_7 = L_9;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___U3CdzU3E5__3_7), (void*)L_9);
		// for (int i = 0; i < dx.Length; i++)
		__this->___U3CiU3E5__4_8 = 0;
		goto IL_00c0;
	}

IL_0052:
	{
		// var nb = pixelGroup.GetItem(item.gridX + dx[i], item.gridZ + dz[i]);
		GameController_t9B394943D9DA551993B8515B21F692D9B0E00853* L_10 = V_1;
		NullCheck(L_10);
		PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* L_11 = L_10->___pixelGroup_7;
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_12 = __this->___item_4;
		NullCheck(L_12);
		int32_t L_13 = L_12->___gridX_5;
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_14 = __this->___U3CdxU3E5__2_6;
		int32_t L_15 = __this->___U3CiU3E5__4_8;
		NullCheck(L_14);
		int32_t L_16 = L_15;
		int32_t L_17 = (L_14)->GetAt(static_cast<il2cpp_array_size_t>(L_16));
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_18 = __this->___item_4;
		NullCheck(L_18);
		int32_t L_19 = L_18->___gridZ_6;
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_20 = __this->___U3CdzU3E5__3_7;
		int32_t L_21 = __this->___U3CiU3E5__4_8;
		NullCheck(L_20);
		int32_t L_22 = L_21;
		int32_t L_23 = (L_20)->GetAt(static_cast<il2cpp_array_size_t>(L_22));
		NullCheck(L_11);
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_24;
		L_24 = PixelGroup_GetItem_mDD0EEC5AAF354352C1CC8B66BE3C59FD78F4B8DE(L_11, ((int32_t)il2cpp_codegen_add(L_13, L_17)), ((int32_t)il2cpp_codegen_add(L_19, L_23)), NULL);
		V_2 = L_24;
		// if (nb != null)
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_25 = V_2;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_26;
		L_26 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_25, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_26)
		{
			goto IL_00b0;
		}
	}
	{
		// yield return nb;
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_27 = V_2;
		__this->___U3CU3E2__current_1 = L_27;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___U3CU3E2__current_1), (void*)L_27);
		__this->___U3CU3E1__state_0 = 1;
		return (bool)1;
	}

IL_00a9:
	{
		__this->___U3CU3E1__state_0 = (-1);
	}

IL_00b0:
	{
		// for (int i = 0; i < dx.Length; i++)
		int32_t L_28 = __this->___U3CiU3E5__4_8;
		V_3 = L_28;
		int32_t L_29 = V_3;
		__this->___U3CiU3E5__4_8 = ((int32_t)il2cpp_codegen_add(L_29, 1));
	}

IL_00c0:
	{
		// for (int i = 0; i < dx.Length; i++)
		int32_t L_30 = __this->___U3CiU3E5__4_8;
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_31 = __this->___U3CdxU3E5__2_6;
		NullCheck(L_31);
		if ((((int32_t)L_30) < ((int32_t)((int32_t)(((RuntimeArray*)L_31)->max_length)))))
		{
			goto IL_0052;
		}
	}
	{
		// }
		return (bool)0;
	}
}
// CrowdMatch.PixelItem CrowdMatch.GameController/<GetNeighbors>d__43::System.Collections.Generic.IEnumerator<CrowdMatch.PixelItem>.get_Current()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* U3CGetNeighborsU3Ed__43_System_Collections_Generic_IEnumeratorU3CCrowdMatch_PixelItemU3E_get_Current_mCA841A32668E7EC56FCC166A6A5643E6A6D47143 (U3CGetNeighborsU3Ed__43_t7EE77D676072170DE792051688D0316271D3643C* __this, const RuntimeMethod* method) 
{
	{
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_0 = __this->___U3CU3E2__current_1;
		return L_0;
	}
}
// System.Void CrowdMatch.GameController/<GetNeighbors>d__43::System.Collections.IEnumerator.Reset()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void U3CGetNeighborsU3Ed__43_System_Collections_IEnumerator_Reset_mD3F0A59DA69CFA678E7F7F98A2E9C717B550A935 (U3CGetNeighborsU3Ed__43_t7EE77D676072170DE792051688D0316271D3643C* __this, const RuntimeMethod* method) 
{
	{
		NotSupportedException_t1429765983D409BD2986508963C98D214E4EBF4A* L_0 = (NotSupportedException_t1429765983D409BD2986508963C98D214E4EBF4A*)il2cpp_codegen_object_new(((RuntimeClass*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&NotSupportedException_t1429765983D409BD2986508963C98D214E4EBF4A_il2cpp_TypeInfo_var)));
		NullCheck(L_0);
		NotSupportedException__ctor_m1398D0CDE19B36AA3DE9392879738C1EA2439CDF(L_0, NULL);
		IL2CPP_RAISE_MANAGED_EXCEPTION(L_0, ((RuntimeMethod*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&U3CGetNeighborsU3Ed__43_System_Collections_IEnumerator_Reset_mD3F0A59DA69CFA678E7F7F98A2E9C717B550A935_RuntimeMethod_var)));
	}
}
// System.Object CrowdMatch.GameController/<GetNeighbors>d__43::System.Collections.IEnumerator.get_Current()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* U3CGetNeighborsU3Ed__43_System_Collections_IEnumerator_get_Current_m22863DC508513CEDC0761406055A22DBA12C1899 (U3CGetNeighborsU3Ed__43_t7EE77D676072170DE792051688D0316271D3643C* __this, const RuntimeMethod* method) 
{
	{
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_0 = __this->___U3CU3E2__current_1;
		return L_0;
	}
}
// System.Collections.Generic.IEnumerator`1<CrowdMatch.PixelItem> CrowdMatch.GameController/<GetNeighbors>d__43::System.Collections.Generic.IEnumerable<CrowdMatch.PixelItem>.GetEnumerator()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* U3CGetNeighborsU3Ed__43_System_Collections_Generic_IEnumerableU3CCrowdMatch_PixelItemU3E_GetEnumerator_mE5B4928F05E765D5395B3FF53C3356C75A8B975F (U3CGetNeighborsU3Ed__43_t7EE77D676072170DE792051688D0316271D3643C* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&U3CGetNeighborsU3Ed__43_t7EE77D676072170DE792051688D0316271D3643C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	U3CGetNeighborsU3Ed__43_t7EE77D676072170DE792051688D0316271D3643C* V_0 = NULL;
	{
		int32_t L_0 = __this->___U3CU3E1__state_0;
		if ((!(((uint32_t)L_0) == ((uint32_t)((int32_t)-2)))))
		{
			goto IL_0022;
		}
	}
	{
		int32_t L_1 = __this->___U3CU3El__initialThreadId_2;
		int32_t L_2;
		L_2 = Environment_get_CurrentManagedThreadId_m66483AADCCC13272EBDCD94D31D2E52603C24BDF(NULL);
		if ((!(((uint32_t)L_1) == ((uint32_t)L_2))))
		{
			goto IL_0022;
		}
	}
	{
		__this->___U3CU3E1__state_0 = 0;
		V_0 = __this;
		goto IL_0035;
	}

IL_0022:
	{
		U3CGetNeighborsU3Ed__43_t7EE77D676072170DE792051688D0316271D3643C* L_3 = (U3CGetNeighborsU3Ed__43_t7EE77D676072170DE792051688D0316271D3643C*)il2cpp_codegen_object_new(U3CGetNeighborsU3Ed__43_t7EE77D676072170DE792051688D0316271D3643C_il2cpp_TypeInfo_var);
		NullCheck(L_3);
		U3CGetNeighborsU3Ed__43__ctor_m715F3C12C7FEB137BF9A1396437FD0D1D2120204(L_3, 0, NULL);
		V_0 = L_3;
		U3CGetNeighborsU3Ed__43_t7EE77D676072170DE792051688D0316271D3643C* L_4 = V_0;
		GameController_t9B394943D9DA551993B8515B21F692D9B0E00853* L_5 = __this->___U3CU3E4__this_3;
		NullCheck(L_4);
		L_4->___U3CU3E4__this_3 = L_5;
		Il2CppCodeGenWriteBarrier((void**)(&L_4->___U3CU3E4__this_3), (void*)L_5);
	}

IL_0035:
	{
		U3CGetNeighborsU3Ed__43_t7EE77D676072170DE792051688D0316271D3643C* L_6 = V_0;
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_7 = __this->___U3CU3E3__item_5;
		NullCheck(L_6);
		L_6->___item_4 = L_7;
		Il2CppCodeGenWriteBarrier((void**)(&L_6->___item_4), (void*)L_7);
		U3CGetNeighborsU3Ed__43_t7EE77D676072170DE792051688D0316271D3643C* L_8 = V_0;
		return L_8;
	}
}
// System.Collections.IEnumerator CrowdMatch.GameController/<GetNeighbors>d__43::System.Collections.IEnumerable.GetEnumerator()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* U3CGetNeighborsU3Ed__43_System_Collections_IEnumerable_GetEnumerator_mE0D89D8DB99A06E579CEA7AF0BF2FA30F88EE72B (U3CGetNeighborsU3Ed__43_t7EE77D676072170DE792051688D0316271D3643C* __this, const RuntimeMethod* method) 
{
	{
		RuntimeObject* L_0;
		L_0 = U3CGetNeighborsU3Ed__43_System_Collections_Generic_IEnumerableU3CCrowdMatch_PixelItemU3E_GetEnumerator_mE5B4928F05E765D5395B3FF53C3356C75A8B975F(__this, NULL);
		return L_0;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
// System.Void CrowdMatch.GameController/<MoveToGatherPoint>d__45::.ctor(System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void U3CMoveToGatherPointU3Ed__45__ctor_mA5A3CB3BF55708EEA1B0073BFD7A68C07BD08C88 (U3CMoveToGatherPointU3Ed__45_t540C804CD76258EA246336B338FC828D346240AF* __this, int32_t ___U3CU3E1__state0, const RuntimeMethod* method) 
{
	{
		Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2(__this, NULL);
		int32_t L_0 = ___U3CU3E1__state0;
		__this->___U3CU3E1__state_0 = L_0;
		return;
	}
}
// System.Void CrowdMatch.GameController/<MoveToGatherPoint>d__45::System.IDisposable.Dispose()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void U3CMoveToGatherPointU3Ed__45_System_IDisposable_Dispose_mDE557BC1ED22214D98A385296D7FDE72D85AB8EC (U3CMoveToGatherPointU3Ed__45_t540C804CD76258EA246336B338FC828D346240AF* __this, const RuntimeMethod* method) 
{
	{
		return;
	}
}
// System.Boolean CrowdMatch.GameController/<MoveToGatherPoint>d__45::MoveNext()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool U3CMoveToGatherPointU3Ed__45_MoveNext_m701AE0708C9AEC6530AAC8A1F97A1810BE762CBE (U3CMoveToGatherPointU3Ed__45_t540C804CD76258EA246336B338FC828D346240AF* __this, const RuntimeMethod* method) 
{
	int32_t V_0 = 0;
	GameController_t9B394943D9DA551993B8515B21F692D9B0E00853* V_1 = NULL;
	float V_2 = 0.0f;
	U3CMoveToGatherPointU3Ed__45_t540C804CD76258EA246336B338FC828D346240AF* G_B5_0 = NULL;
	U3CMoveToGatherPointU3Ed__45_t540C804CD76258EA246336B338FC828D346240AF* G_B4_0 = NULL;
	float G_B6_0 = 0.0f;
	U3CMoveToGatherPointU3Ed__45_t540C804CD76258EA246336B338FC828D346240AF* G_B6_1 = NULL;
	float G_B10_0 = 0.0f;
	{
		int32_t L_0 = __this->___U3CU3E1__state_0;
		V_0 = L_0;
		GameController_t9B394943D9DA551993B8515B21F692D9B0E00853* L_1 = __this->___U3CU3E4__this_3;
		V_1 = L_1;
		int32_t L_2 = V_0;
		if (!L_2)
		{
			goto IL_001a;
		}
	}
	{
		int32_t L_3 = V_0;
		if ((((int32_t)L_3) == ((int32_t)1)))
		{
			goto IL_00ed;
		}
	}
	{
		return (bool)0;
	}

IL_001a:
	{
		__this->___U3CU3E1__state_0 = (-1);
		// Vector3 start = item.transform.localPosition;
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_4 = __this->___item_2;
		NullCheck(L_4);
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_5;
		L_5 = Component_get_transform_m2919A1D81931E6932C7F06D4C2F0AB8DDA9A5371(L_4, NULL);
		NullCheck(L_5);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_6;
		L_6 = Transform_get_localPosition_mA9C86B990DF0685EA1061A120218993FDCC60A95(L_5, NULL);
		__this->___U3CstartU3E5__2_4 = L_6;
		// Vector3 target = RandomGatherTarget();
		GameController_t9B394943D9DA551993B8515B21F692D9B0E00853* L_7 = V_1;
		NullCheck(L_7);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_8;
		L_8 = GameController_RandomGatherTarget_m44DC3062E3FCE4FE5F9416A9CA86E46DB2EF786C(L_7, NULL);
		__this->___U3CtargetU3E5__3_5 = L_8;
		// float duration = gatherSpeed > 0.0001f
		//     ? Vector3.Distance(start, target) / gatherSpeed
		//     : 0f;
		GameController_t9B394943D9DA551993B8515B21F692D9B0E00853* L_9 = V_1;
		NullCheck(L_9);
		float L_10 = L_9->___gatherSpeed_9;
		G_B4_0 = __this;
		if ((((float)L_10) > ((float)(9.99999975E-05f))))
		{
			G_B5_0 = __this;
			goto IL_0058;
		}
	}
	{
		G_B6_0 = (0.0f);
		G_B6_1 = G_B4_0;
		goto IL_0070;
	}

IL_0058:
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_11 = __this->___U3CstartU3E5__2_4;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_12 = __this->___U3CtargetU3E5__3_5;
		float L_13;
		L_13 = Vector3_Distance_m2314DB9B8BD01157E013DF87BEA557375C7F9FF9_inline(L_11, L_12, NULL);
		GameController_t9B394943D9DA551993B8515B21F692D9B0E00853* L_14 = V_1;
		NullCheck(L_14);
		float L_15 = L_14->___gatherSpeed_9;
		G_B6_0 = ((float)(L_13/L_15));
		G_B6_1 = G_B5_0;
	}

IL_0070:
	{
		NullCheck(G_B6_1);
		G_B6_1->___U3CdurationU3E5__4_6 = G_B6_0;
		// float t = 0f;
		__this->___U3CtU3E5__5_7 = (0.0f);
		goto IL_00f4;
	}

IL_0082:
	{
		// t += Time.deltaTime;
		float L_16 = __this->___U3CtU3E5__5_7;
		float L_17;
		L_17 = Time_get_deltaTime_mC3195000401F0FD167DD2F948FD2BC58330D0865(NULL);
		__this->___U3CtU3E5__5_7 = ((float)il2cpp_codegen_add(L_16, L_17));
		// float k = Mathf.Clamp01(duration > 0.0001f ? t / duration : 1f);
		float L_18 = __this->___U3CdurationU3E5__4_6;
		if ((((float)L_18) > ((float)(9.99999975E-05f))))
		{
			goto IL_00a8;
		}
	}
	{
		G_B10_0 = (1.0f);
		goto IL_00b5;
	}

IL_00a8:
	{
		float L_19 = __this->___U3CtU3E5__5_7;
		float L_20 = __this->___U3CdurationU3E5__4_6;
		G_B10_0 = ((float)(L_19/L_20));
	}

IL_00b5:
	{
		float L_21;
		L_21 = Mathf_Clamp01_mA7E048DBDA832D399A581BE4D6DED9FA44CE0F14_inline(G_B10_0, NULL);
		V_2 = L_21;
		// item.transform.localPosition = Vector3.Lerp(start, target, k);
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_22 = __this->___item_2;
		NullCheck(L_22);
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_23;
		L_23 = Component_get_transform_m2919A1D81931E6932C7F06D4C2F0AB8DDA9A5371(L_22, NULL);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_24 = __this->___U3CstartU3E5__2_4;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_25 = __this->___U3CtargetU3E5__3_5;
		float L_26 = V_2;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_27;
		L_27 = Vector3_Lerp_m3A906D0530A94FAABB94F0F905E84D99BE85C3F8_inline(L_24, L_25, L_26, NULL);
		NullCheck(L_23);
		Transform_set_localPosition_mDE1C997F7D79C0885210B7732B4BA50EE7D73134(L_23, L_27, NULL);
		// yield return null;
		__this->___U3CU3E2__current_1 = NULL;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___U3CU3E2__current_1), (void*)NULL);
		__this->___U3CU3E1__state_0 = 1;
		return (bool)1;
	}

IL_00ed:
	{
		__this->___U3CU3E1__state_0 = (-1);
	}

IL_00f4:
	{
		// while (t < duration)
		float L_28 = __this->___U3CtU3E5__5_7;
		float L_29 = __this->___U3CdurationU3E5__4_6;
		if ((((float)L_28) < ((float)L_29)))
		{
			goto IL_0082;
		}
	}
	{
		// item.transform.localPosition = target;
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_30 = __this->___item_2;
		NullCheck(L_30);
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_31;
		L_31 = Component_get_transform_m2919A1D81931E6932C7F06D4C2F0AB8DDA9A5371(L_30, NULL);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_32 = __this->___U3CtargetU3E5__3_5;
		NullCheck(L_31);
		Transform_set_localPosition_mDE1C997F7D79C0885210B7732B4BA50EE7D73134(L_31, L_32, NULL);
		// item.arrivedAtGatherPoint = true;
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_33 = __this->___item_2;
		NullCheck(L_33);
		L_33->___arrivedAtGatherPoint_20 = (bool)1;
		// item.SetWalking(false);   // ?????????? ? Idle??????????
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_34 = __this->___item_2;
		NullCheck(L_34);
		PixelItem_SetWalking_mEA83F37D0924BFD82287B01679016B81CAB96C50(L_34, (bool)0, NULL);
		// }
		return (bool)0;
	}
}
// System.Object CrowdMatch.GameController/<MoveToGatherPoint>d__45::System.Collections.Generic.IEnumerator<System.Object>.get_Current()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* U3CMoveToGatherPointU3Ed__45_System_Collections_Generic_IEnumeratorU3CSystem_ObjectU3E_get_Current_m3AE6DC28006B718D7A682591B4775552D692C666 (U3CMoveToGatherPointU3Ed__45_t540C804CD76258EA246336B338FC828D346240AF* __this, const RuntimeMethod* method) 
{
	{
		RuntimeObject* L_0 = __this->___U3CU3E2__current_1;
		return L_0;
	}
}
// System.Void CrowdMatch.GameController/<MoveToGatherPoint>d__45::System.Collections.IEnumerator.Reset()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void U3CMoveToGatherPointU3Ed__45_System_Collections_IEnumerator_Reset_m3579E49E6A5A1582EDE1A9027914E9A69863DC76 (U3CMoveToGatherPointU3Ed__45_t540C804CD76258EA246336B338FC828D346240AF* __this, const RuntimeMethod* method) 
{
	{
		NotSupportedException_t1429765983D409BD2986508963C98D214E4EBF4A* L_0 = (NotSupportedException_t1429765983D409BD2986508963C98D214E4EBF4A*)il2cpp_codegen_object_new(((RuntimeClass*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&NotSupportedException_t1429765983D409BD2986508963C98D214E4EBF4A_il2cpp_TypeInfo_var)));
		NullCheck(L_0);
		NotSupportedException__ctor_m1398D0CDE19B36AA3DE9392879738C1EA2439CDF(L_0, NULL);
		IL2CPP_RAISE_MANAGED_EXCEPTION(L_0, ((RuntimeMethod*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&U3CMoveToGatherPointU3Ed__45_System_Collections_IEnumerator_Reset_m3579E49E6A5A1582EDE1A9027914E9A69863DC76_RuntimeMethod_var)));
	}
}
// System.Object CrowdMatch.GameController/<MoveToGatherPoint>d__45::System.Collections.IEnumerator.get_Current()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* U3CMoveToGatherPointU3Ed__45_System_Collections_IEnumerator_get_Current_mB4C4F9C178FB5E4ADFBB9D922197F354D7DE729A (U3CMoveToGatherPointU3Ed__45_t540C804CD76258EA246336B338FC828D346240AF* __this, const RuntimeMethod* method) 
{
	{
		RuntimeObject* L_0 = __this->___U3CU3E2__current_1;
		return L_0;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
// System.Int32 CrowdMatch.GridColorMatcher::FindClosestColorIndex(UnityEngine.Color,UnityEngine.Color[])
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t GridColorMatcher_FindClosestColorIndex_mC714B659FAA530B6286BC986B6B3FD98645B5729 (Color_tD001788D726C3A7F1379BEED0260B9591F440C1F ___target0, ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* ___palette1, const RuntimeMethod* method) 
{
	int32_t V_0 = 0;
	float V_1 = 0.0f;
	int32_t V_2 = 0;
	float V_3 = 0.0f;
	float V_4 = 0.0f;
	float V_5 = 0.0f;
	{
		// if (palette == null || palette.Length == 0) return -1;
		ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* L_0 = ___palette1;
		if (!L_0)
		{
			goto IL_0007;
		}
	}
	{
		ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* L_1 = ___palette1;
		NullCheck(L_1);
		if ((((RuntimeArray*)L_1)->max_length))
		{
			goto IL_0009;
		}
	}

IL_0007:
	{
		// if (palette == null || palette.Length == 0) return -1;
		return (-1);
	}

IL_0009:
	{
		// int bestIdx = 0;
		V_0 = 0;
		// float bestDist = float.MaxValue;
		V_1 = ((std::numeric_limits<float>::max)());
		// for (int i = 0; i < palette.Length; i++)
		V_2 = 0;
		goto IL_007f;
	}

IL_0015:
	{
		// float dr = target.r - palette[i].r;
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_2 = ___target0;
		float L_3 = L_2.___r_0;
		ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* L_4 = ___palette1;
		int32_t L_5 = V_2;
		NullCheck(L_4);
		float L_6 = ((L_4)->GetAddressAt(static_cast<il2cpp_array_size_t>(L_5)))->___r_0;
		// float dg = target.g - palette[i].g;
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_7 = ___target0;
		float L_8 = L_7.___g_1;
		ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* L_9 = ___palette1;
		int32_t L_10 = V_2;
		NullCheck(L_9);
		float L_11 = ((L_9)->GetAddressAt(static_cast<il2cpp_array_size_t>(L_10)))->___g_1;
		V_3 = ((float)il2cpp_codegen_subtract(L_8, L_11));
		// float db = target.b - palette[i].b;
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_12 = ___target0;
		float L_13 = L_12.___b_2;
		ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* L_14 = ___palette1;
		int32_t L_15 = V_2;
		NullCheck(L_14);
		float L_16 = ((L_14)->GetAddressAt(static_cast<il2cpp_array_size_t>(L_15)))->___b_2;
		V_4 = ((float)il2cpp_codegen_subtract(L_13, L_16));
		// float dist = dr * dr * 2f + dg * dg * 4f + db * db * 3f;
		float L_17 = ((float)il2cpp_codegen_subtract(L_3, L_6));
		float L_18 = V_3;
		float L_19 = V_3;
		float L_20 = V_4;
		float L_21 = V_4;
		V_5 = ((float)il2cpp_codegen_add(((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_multiply(L_17, L_17)), (2.0f))), ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_multiply(L_18, L_19)), (4.0f))))), ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_multiply(L_20, L_21)), (3.0f)))));
		// if (dist < bestDist)
		float L_22 = V_5;
		float L_23 = V_1;
		if ((!(((float)L_22) < ((float)L_23))))
		{
			goto IL_007b;
		}
	}
	{
		// bestDist = dist;
		float L_24 = V_5;
		V_1 = L_24;
		// bestIdx = i;
		int32_t L_25 = V_2;
		V_0 = L_25;
	}

IL_007b:
	{
		// for (int i = 0; i < palette.Length; i++)
		int32_t L_26 = V_2;
		V_2 = ((int32_t)il2cpp_codegen_add(L_26, 1));
	}

IL_007f:
	{
		// for (int i = 0; i < palette.Length; i++)
		int32_t L_27 = V_2;
		ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* L_28 = ___palette1;
		NullCheck(L_28);
		if ((((int32_t)L_27) < ((int32_t)((int32_t)(((RuntimeArray*)L_28)->max_length)))))
		{
			goto IL_0015;
		}
	}
	{
		// return bestIdx;
		int32_t L_29 = V_0;
		return L_29;
	}
}
// System.Boolean CrowdMatch.GridColorMatcher::IsPureWhite(UnityEngine.Color)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool GridColorMatcher_IsPureWhite_m90290A652356A34198582406FF55CF2404DD072A (Color_tD001788D726C3A7F1379BEED0260B9591F440C1F ___c0, const RuntimeMethod* method) 
{
	{
		// return Mathf.Approximately(c.r, 1f)
		//     && Mathf.Approximately(c.g, 1f)
		//     && Mathf.Approximately(c.b, 1f);
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_0 = ___c0;
		float L_1 = L_0.___r_0;
		bool L_2;
		L_2 = Mathf_Approximately_m1DADD012A8FC82E11FB282501AE2EBBF9A77150B_inline(L_1, (1.0f), NULL);
		if (!L_2)
		{
			goto IL_0035;
		}
	}
	{
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_3 = ___c0;
		float L_4 = L_3.___g_1;
		bool L_5;
		L_5 = Mathf_Approximately_m1DADD012A8FC82E11FB282501AE2EBBF9A77150B_inline(L_4, (1.0f), NULL);
		if (!L_5)
		{
			goto IL_0035;
		}
	}
	{
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_6 = ___c0;
		float L_7 = L_6.___b_2;
		bool L_8;
		L_8 = Mathf_Approximately_m1DADD012A8FC82E11FB282501AE2EBBF9A77150B_inline(L_7, (1.0f), NULL);
		return L_8;
	}

IL_0035:
	{
		return (bool)0;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
// System.Void CrowdMatch.LevelSkipButtons::Awake()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void LevelSkipButtons_Awake_mA4A21709A45C218C16C31211145DE5EFCCD18568 (LevelSkipButtons_t116E0448EB9F81B180CA7AA70DCB08A06728621D* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&LevelSkipButtons_NextLevel_m6BC706B486985644D627D363EE506DBBDB9E4C78_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&LevelSkipButtons_PrevLevel_m7734B71E7CF419EF9F4AE420091E2F57A52A00AC_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&UnityAction_t11A1F3B953B365C072A5DCC32677EE1796A962A7_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral2FB2883B7696560B5CEC12690102A0B2BE1338A9);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral562761ED06D88AFEA8594A1A352122119A54DAC9);
		s_Il2CppMethodInitialized = true;
	}
	{
		// if (nextButton == null)
		Button_t6786514A57F7AFDEE5431112FEA0CAB24F5AE098* L_0 = __this->___nextButton_4;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_1;
		L_1 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_0, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_1)
		{
			goto IL_001e;
		}
	}
	{
		// nextButton = FindButton("NextLvl");
		Button_t6786514A57F7AFDEE5431112FEA0CAB24F5AE098* L_2;
		L_2 = LevelSkipButtons_FindButton_m45658843F17679CBF404FFC1B70ABC044427D53F(_stringLiteral2FB2883B7696560B5CEC12690102A0B2BE1338A9, NULL);
		__this->___nextButton_4 = L_2;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___nextButton_4), (void*)L_2);
	}

IL_001e:
	{
		// if (prevButton == null)
		Button_t6786514A57F7AFDEE5431112FEA0CAB24F5AE098* L_3 = __this->___prevButton_5;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_4;
		L_4 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_3, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_4)
		{
			goto IL_003c;
		}
	}
	{
		// prevButton = FindButton("PrevLvl");
		Button_t6786514A57F7AFDEE5431112FEA0CAB24F5AE098* L_5;
		L_5 = LevelSkipButtons_FindButton_m45658843F17679CBF404FFC1B70ABC044427D53F(_stringLiteral562761ED06D88AFEA8594A1A352122119A54DAC9, NULL);
		__this->___prevButton_5 = L_5;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___prevButton_5), (void*)L_5);
	}

IL_003c:
	{
		// if (nextButton != null)
		Button_t6786514A57F7AFDEE5431112FEA0CAB24F5AE098* L_6 = __this->___nextButton_4;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_7;
		L_7 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_6, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_7)
		{
			goto IL_0066;
		}
	}
	{
		// nextButton.onClick.AddListener(NextLevel);
		Button_t6786514A57F7AFDEE5431112FEA0CAB24F5AE098* L_8 = __this->___nextButton_4;
		NullCheck(L_8);
		ButtonClickedEvent_t8EA72E90B3BD1392FB3B3EF167D5121C23569E4C* L_9;
		L_9 = Button_get_onClick_m701712A7F7F000CC80D517C4510697E15722C35C_inline(L_8, NULL);
		UnityAction_t11A1F3B953B365C072A5DCC32677EE1796A962A7* L_10 = (UnityAction_t11A1F3B953B365C072A5DCC32677EE1796A962A7*)il2cpp_codegen_object_new(UnityAction_t11A1F3B953B365C072A5DCC32677EE1796A962A7_il2cpp_TypeInfo_var);
		NullCheck(L_10);
		UnityAction__ctor_mC53E20D6B66E0D5688CD81B88DBB34F5A58B7131(L_10, __this, (intptr_t)((void*)LevelSkipButtons_NextLevel_m6BC706B486985644D627D363EE506DBBDB9E4C78_RuntimeMethod_var), NULL);
		NullCheck(L_9);
		UnityEvent_AddListener_m8AA4287C16628486B41DA41CA5E7A856A706D302(L_9, L_10, NULL);
	}

IL_0066:
	{
		// if (prevButton != null)
		Button_t6786514A57F7AFDEE5431112FEA0CAB24F5AE098* L_11 = __this->___prevButton_5;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_12;
		L_12 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_11, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_12)
		{
			goto IL_0090;
		}
	}
	{
		// prevButton.onClick.AddListener(PrevLevel);
		Button_t6786514A57F7AFDEE5431112FEA0CAB24F5AE098* L_13 = __this->___prevButton_5;
		NullCheck(L_13);
		ButtonClickedEvent_t8EA72E90B3BD1392FB3B3EF167D5121C23569E4C* L_14;
		L_14 = Button_get_onClick_m701712A7F7F000CC80D517C4510697E15722C35C_inline(L_13, NULL);
		UnityAction_t11A1F3B953B365C072A5DCC32677EE1796A962A7* L_15 = (UnityAction_t11A1F3B953B365C072A5DCC32677EE1796A962A7*)il2cpp_codegen_object_new(UnityAction_t11A1F3B953B365C072A5DCC32677EE1796A962A7_il2cpp_TypeInfo_var);
		NullCheck(L_15);
		UnityAction__ctor_mC53E20D6B66E0D5688CD81B88DBB34F5A58B7131(L_15, __this, (intptr_t)((void*)LevelSkipButtons_PrevLevel_m7734B71E7CF419EF9F4AE420091E2F57A52A00AC_RuntimeMethod_var), NULL);
		NullCheck(L_14);
		UnityEvent_AddListener_m8AA4287C16628486B41DA41CA5E7A856A706D302(L_14, L_15, NULL);
	}

IL_0090:
	{
		// }
		return;
	}
}
// UnityEngine.UI.Button CrowdMatch.LevelSkipButtons::FindButton(System.String)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Button_t6786514A57F7AFDEE5431112FEA0CAB24F5AE098* LevelSkipButtons_FindButton_m45658843F17679CBF404FFC1B70ABC044427D53F (String_t* ___name0, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&GameObject_GetComponent_TisButton_t6786514A57F7AFDEE5431112FEA0CAB24F5AE098_mB997CBF78A37938DC1624352E12D0205078CB290_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* V_0 = NULL;
	{
		// var go = GameObject.Find(name);
		String_t* L_0 = ___name0;
		GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_1;
		L_1 = GameObject_Find_m7A669B4EEC2617AB82F6E3FF007CDCD9F21DB300(L_0, NULL);
		V_0 = L_1;
		// return go != null ? go.GetComponent<Button>() : null;
		GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_2 = V_0;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_3;
		L_3 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_2, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (L_3)
		{
			goto IL_0012;
		}
	}
	{
		return (Button_t6786514A57F7AFDEE5431112FEA0CAB24F5AE098*)NULL;
	}

IL_0012:
	{
		GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_4 = V_0;
		NullCheck(L_4);
		Button_t6786514A57F7AFDEE5431112FEA0CAB24F5AE098* L_5;
		L_5 = GameObject_GetComponent_TisButton_t6786514A57F7AFDEE5431112FEA0CAB24F5AE098_mB997CBF78A37938DC1624352E12D0205078CB290(L_4, GameObject_GetComponent_TisButton_t6786514A57F7AFDEE5431112FEA0CAB24F5AE098_mB997CBF78A37938DC1624352E12D0205078CB290_RuntimeMethod_var);
		return L_5;
	}
}
// System.Void CrowdMatch.LevelSkipButtons::NextLevel()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void LevelSkipButtons_NextLevel_m6BC706B486985644D627D363EE506DBBDB9E4C78 (LevelSkipButtons_t116E0448EB9F81B180CA7AA70DCB08A06728621D* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	GameManager_t5B23045F478AD8B0E3D943ECC8493384B02FF32F* V_0 = NULL;
	{
		// var gm = GameManager.Instance;
		GameManager_t5B23045F478AD8B0E3D943ECC8493384B02FF32F* L_0;
		L_0 = GameManager_get_Instance_m5F7736EF916BFD34C734BE27B0EA4760C2D545FA_inline(NULL);
		V_0 = L_0;
		// if (gm != null)
		GameManager_t5B23045F478AD8B0E3D943ECC8493384B02FF32F* L_1 = V_0;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_2;
		L_2 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_1, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_2)
		{
			goto IL_0015;
		}
	}
	{
		// gm.GameWin();
		GameManager_t5B23045F478AD8B0E3D943ECC8493384B02FF32F* L_3 = V_0;
		NullCheck(L_3);
		GameManager_GameWin_m65A28284A87BCEEAE65E48A161A1C5D81B71B9A4(L_3, NULL);
	}

IL_0015:
	{
		// }
		return;
	}
}
// System.Void CrowdMatch.LevelSkipButtons::PrevLevel()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void LevelSkipButtons_PrevLevel_m7734B71E7CF419EF9F4AE420091E2F57A52A00AC (LevelSkipButtons_t116E0448EB9F81B180CA7AA70DCB08A06728621D* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	GameManager_t5B23045F478AD8B0E3D943ECC8493384B02FF32F* V_0 = NULL;
	{
		// var gm = GameManager.Instance;
		GameManager_t5B23045F478AD8B0E3D943ECC8493384B02FF32F* L_0;
		L_0 = GameManager_get_Instance_m5F7736EF916BFD34C734BE27B0EA4760C2D545FA_inline(NULL);
		V_0 = L_0;
		// if (gm != null)
		GameManager_t5B23045F478AD8B0E3D943ECC8493384B02FF32F* L_1 = V_0;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_2;
		L_2 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_1, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_2)
		{
			goto IL_0015;
		}
	}
	{
		// gm.PrevLevel();
		GameManager_t5B23045F478AD8B0E3D943ECC8493384B02FF32F* L_3 = V_0;
		NullCheck(L_3);
		GameManager_PrevLevel_m53AEC4E3BF586BAAF764D5F175322FB30F17FCFC(L_3, NULL);
	}

IL_0015:
	{
		// }
		return;
	}
}
// System.Void CrowdMatch.LevelSkipButtons::.ctor()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void LevelSkipButtons__ctor_m3A0D84A34896DE899896B320F596F1AE0713A1D1 (LevelSkipButtons_t116E0448EB9F81B180CA7AA70DCB08A06728621D* __this, const RuntimeMethod* method) 
{
	{
		MonoBehaviour__ctor_m592DB0105CA0BC97AA1C5F4AD27B12D68A3B7C1E(__this, NULL);
		return;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
// UnityEngine.BoxCollider CrowdMatch.PixelClickListener::get_BoxCollider()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR BoxCollider_tFA5D239388334D6DE0B8FFDAD6825C5B03786E23* PixelClickListener_get_BoxCollider_m9BC4090DD0238F70A3BD78FA280683C138C66C63 (PixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524* __this, const RuntimeMethod* method) 
{
	{
		// public BoxCollider BoxCollider { get; private set; }
		BoxCollider_tFA5D239388334D6DE0B8FFDAD6825C5B03786E23* L_0 = __this->___U3CBoxColliderU3Ek__BackingField_5;
		return L_0;
	}
}
// System.Void CrowdMatch.PixelClickListener::set_BoxCollider(UnityEngine.BoxCollider)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelClickListener_set_BoxCollider_m103128AD33A18949AC8C92230E2451E44A68D847 (PixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524* __this, BoxCollider_tFA5D239388334D6DE0B8FFDAD6825C5B03786E23* ___value0, const RuntimeMethod* method) 
{
	{
		// public BoxCollider BoxCollider { get; private set; }
		BoxCollider_tFA5D239388334D6DE0B8FFDAD6825C5B03786E23* L_0 = ___value0;
		__this->___U3CBoxColliderU3Ek__BackingField_5 = L_0;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___U3CBoxColliderU3Ek__BackingField_5), (void*)L_0);
		return;
	}
}
// System.Void CrowdMatch.PixelClickListener::Awake()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelClickListener_Awake_m020653AF5A56FB36B8B0D9DBB165EB86B7E182A6 (PixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Component_GetComponent_TisBoxCollider_tFA5D239388334D6DE0B8FFDAD6825C5B03786E23_m59698092F1230C6FB7F40D0F58F643A931A732D7_RuntimeMethod_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		// BoxCollider = GetComponent<BoxCollider>();
		BoxCollider_tFA5D239388334D6DE0B8FFDAD6825C5B03786E23* L_0;
		L_0 = Component_GetComponent_TisBoxCollider_tFA5D239388334D6DE0B8FFDAD6825C5B03786E23_m59698092F1230C6FB7F40D0F58F643A931A732D7(__this, Component_GetComponent_TisBoxCollider_tFA5D239388334D6DE0B8FFDAD6825C5B03786E23_m59698092F1230C6FB7F40D0F58F643A931A732D7_RuntimeMethod_var);
		PixelClickListener_set_BoxCollider_m103128AD33A18949AC8C92230E2451E44A68D847_inline(__this, L_0, NULL);
		// }
		return;
	}
}
// System.Void CrowdMatch.PixelClickListener::SetClickable(System.Boolean)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelClickListener_SetClickable_mC408165A086B268E7D4D348CC90640DD8DCF5EC0 (PixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524* __this, bool ___clickable0, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		// if (BoxCollider != null)
		BoxCollider_tFA5D239388334D6DE0B8FFDAD6825C5B03786E23* L_0;
		L_0 = PixelClickListener_get_BoxCollider_m9BC4090DD0238F70A3BD78FA280683C138C66C63_inline(__this, NULL);
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_1;
		L_1 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_0, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_1)
		{
			goto IL_001a;
		}
	}
	{
		// BoxCollider.enabled = clickable;
		BoxCollider_tFA5D239388334D6DE0B8FFDAD6825C5B03786E23* L_2;
		L_2 = PixelClickListener_get_BoxCollider_m9BC4090DD0238F70A3BD78FA280683C138C66C63_inline(__this, NULL);
		bool L_3 = ___clickable0;
		NullCheck(L_2);
		Collider_set_enabled_m8D5C3B5047592D227A52560FC9723D176E209F70(L_2, L_3, NULL);
	}

IL_001a:
	{
		// }
		return;
	}
}
// System.Void CrowdMatch.PixelClickListener::.ctor()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelClickListener__ctor_mB077A9EB7C4222FB7A93849C2503A2C1A763F559 (PixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524* __this, const RuntimeMethod* method) 
{
	{
		MonoBehaviour__ctor_m592DB0105CA0BC97AA1C5F4AD27B12D68A3B7C1E(__this, NULL);
		return;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
// System.Single CrowdMatch.PixelGroup::get_CellSizeX()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PixelGroup_get_CellSizeX_mFF4EC1FE8D75565520456EB7BD12D9DFCF2AFAE0 (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, const RuntimeMethod* method) 
{
	{
		// public float CellSizeX => unitSize + spacingX;
		float L_0 = __this->___unitSize_4;
		float L_1 = __this->___spacingX_5;
		return ((float)il2cpp_codegen_add(L_0, L_1));
	}
}
// System.Single CrowdMatch.PixelGroup::get_CellSizeZ()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PixelGroup_get_CellSizeZ_m8BD98A189CFE63A89D5928CDE4EB4DD7D681CC50 (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, const RuntimeMethod* method) 
{
	{
		// public float CellSizeZ => unitSize + spacingZ;
		float L_0 = __this->___unitSize_4;
		float L_1 = __this->___spacingZ_6;
		return ((float)il2cpp_codegen_add(L_0, L_1));
	}
}
// System.Int32 CrowdMatch.PixelGroup::get_TotalRows()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t PixelGroup_get_TotalRows_m463568B781F3699B24713B03D51CB2A5084E2D79 (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, const RuntimeMethod* method) 
{
	{
		// public int TotalRows => rows + Mathf.Max(0, tailRows);
		int32_t L_0 = __this->___rows_8;
		int32_t L_1 = __this->___tailRows_9;
		int32_t L_2;
		L_2 = Mathf_Max_m7FA442918DE37E3A00106D1F2E789D65829792B8_inline(0, L_1, NULL);
		return ((int32_t)il2cpp_codegen_add(L_0, L_2));
	}
}
// System.Void CrowdMatch.PixelGroup::Start()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelGroup_Start_mF415692609EF657B9A380994D7D8186AA58968C2 (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, const RuntimeMethod* method) 
{
	{
		// RebuildGrid();
		PixelGroup_RebuildGrid_mDE9E7C6B2933B5C2FDBCE71287AA8ECC2D2510E1(__this, NULL);
		// }
		return;
	}
}
// System.Void CrowdMatch.PixelGroup::RebuildGrid()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelGroup_RebuildGrid_mDE9E7C6B2933B5C2FDBCE71287AA8ECC2D2510E1 (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Component_GetComponentsInChildren_TisPixelItem_t863890C77945A8C08435BF2F8859A47A413C794E_mC6DFA6AEA610A79B3113B66C8457AA3325D29586_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Component_GetComponentsInChildren_TisWallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0_m9AD00368CDD148B6F031F3195DAF4512127EBA59_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IEnumerable_1_t8845214D7CADFAAD7AB98132A368905184A79DDF_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IEnumerator_1_t772680255A1A75379853D3823763B88BF026E055_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IEnumerator_t7B609C2FFA6EB5167D9C62A0C32A21DE2F666DAA_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PixelItemU5BU2CU5D_t26ACEBF9031979698453CA52C61A20C0A1A68C7F_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	PixelItemU5BU5D_t48D4473FA8D417437EB43513F73EC0FD24CBD02F* V_0 = NULL;
	int32_t V_1 = 0;
	PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* V_2 = NULL;
	WallItemU5BU5D_tD1B1EF0B4FAE8376B92DB7B23ACCE192653C9B58* V_3 = NULL;
	WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* V_4 = NULL;
	RuntimeObject* V_5 = NULL;
	Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A V_6;
	memset((&V_6), 0, sizeof(V_6));
	{
		// grid = new PixelItem[columns, TotalRows];
		int32_t L_0 = __this->___columns_7;
		int32_t L_1;
		L_1 = PixelGroup_get_TotalRows_m463568B781F3699B24713B03D51CB2A5084E2D79(__this, NULL);
		il2cpp_array_size_t L_3[] = { (il2cpp_array_size_t)L_0, (il2cpp_array_size_t)L_1 };
		PixelItemU5BU2CU5D_t26ACEBF9031979698453CA52C61A20C0A1A68C7F* L_2 = (PixelItemU5BU2CU5D_t26ACEBF9031979698453CA52C61A20C0A1A68C7F*)GenArrayNew(PixelItemU5BU2CU5D_t26ACEBF9031979698453CA52C61A20C0A1A68C7F_il2cpp_TypeInfo_var, L_3);
		__this->___grid_16 = L_2;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___grid_16), (void*)L_2);
		// wallGrid = new bool[columns, TotalRows];
		int32_t L_4 = __this->___columns_7;
		int32_t L_5;
		L_5 = PixelGroup_get_TotalRows_m463568B781F3699B24713B03D51CB2A5084E2D79(__this, NULL);
		il2cpp_array_size_t L_7[] = { (il2cpp_array_size_t)L_4, (il2cpp_array_size_t)L_5 };
		BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* L_6 = (BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6*)GenArrayNew(BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6_il2cpp_TypeInfo_var, L_7);
		__this->___wallGrid_17 = L_6;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___wallGrid_17), (void*)L_6);
		// foreach (var item in GetComponentsInChildren<PixelItem>())
		PixelItemU5BU5D_t48D4473FA8D417437EB43513F73EC0FD24CBD02F* L_8;
		L_8 = Component_GetComponentsInChildren_TisPixelItem_t863890C77945A8C08435BF2F8859A47A413C794E_mC6DFA6AEA610A79B3113B66C8457AA3325D29586(__this, Component_GetComponentsInChildren_TisPixelItem_t863890C77945A8C08435BF2F8859A47A413C794E_mC6DFA6AEA610A79B3113B66C8457AA3325D29586_RuntimeMethod_var);
		V_0 = L_8;
		V_1 = 0;
		goto IL_0074;
	}

IL_0039:
	{
		// foreach (var item in GetComponentsInChildren<PixelItem>())
		PixelItemU5BU5D_t48D4473FA8D417437EB43513F73EC0FD24CBD02F* L_9 = V_0;
		int32_t L_10 = V_1;
		NullCheck(L_9);
		int32_t L_11 = L_10;
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_12 = (L_9)->GetAt(static_cast<il2cpp_array_size_t>(L_11));
		V_2 = L_12;
		// if (IsInRange(item.gridX, item.gridZ))
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_13 = V_2;
		NullCheck(L_13);
		int32_t L_14 = L_13->___gridX_5;
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_15 = V_2;
		NullCheck(L_15);
		int32_t L_16 = L_15->___gridZ_6;
		bool L_17;
		L_17 = PixelGroup_IsInRange_m7D51A009EE60A9ADDC0707368ADDDE95FCF788C3(__this, L_14, L_16, NULL);
		if (!L_17)
		{
			goto IL_0070;
		}
	}
	{
		// grid[item.gridX, item.gridZ] = item;
		PixelItemU5BU2CU5D_t26ACEBF9031979698453CA52C61A20C0A1A68C7F* L_18 = __this->___grid_16;
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_19 = V_2;
		NullCheck(L_19);
		int32_t L_20 = L_19->___gridX_5;
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_21 = V_2;
		NullCheck(L_21);
		int32_t L_22 = L_21->___gridZ_6;
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_23 = V_2;
		NullCheck(L_18);
		(L_18)->SetAt(L_20, L_22, L_23);
		// item.group = this;
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_24 = V_2;
		NullCheck(L_24);
		L_24->___group_19 = __this;
		Il2CppCodeGenWriteBarrier((void**)(&L_24->___group_19), (void*)__this);
	}

IL_0070:
	{
		int32_t L_25 = V_1;
		V_1 = ((int32_t)il2cpp_codegen_add(L_25, 1));
	}

IL_0074:
	{
		// foreach (var item in GetComponentsInChildren<PixelItem>())
		int32_t L_26 = V_1;
		PixelItemU5BU5D_t48D4473FA8D417437EB43513F73EC0FD24CBD02F* L_27 = V_0;
		NullCheck(L_27);
		if ((((int32_t)L_26) < ((int32_t)((int32_t)(((RuntimeArray*)L_27)->max_length)))))
		{
			goto IL_0039;
		}
	}
	{
		// foreach (var wall in GetComponentsInChildren<WallItem>())
		WallItemU5BU5D_tD1B1EF0B4FAE8376B92DB7B23ACCE192653C9B58* L_28;
		L_28 = Component_GetComponentsInChildren_TisWallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0_m9AD00368CDD148B6F031F3195DAF4512127EBA59(__this, Component_GetComponentsInChildren_TisWallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0_m9AD00368CDD148B6F031F3195DAF4512127EBA59_RuntimeMethod_var);
		V_3 = L_28;
		V_1 = 0;
		goto IL_0100;
	}

IL_0085:
	{
		// foreach (var wall in GetComponentsInChildren<WallItem>())
		WallItemU5BU5D_tD1B1EF0B4FAE8376B92DB7B23ACCE192653C9B58* L_29 = V_3;
		int32_t L_30 = V_1;
		NullCheck(L_29);
		int32_t L_31 = L_30;
		WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* L_32 = (L_29)->GetAt(static_cast<il2cpp_array_size_t>(L_31));
		V_4 = L_32;
		// if (wall == null)
		WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* L_33 = V_4;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_34;
		L_34 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_33, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (L_34)
		{
			goto IL_00fc;
		}
	}
	{
		// wall.group = this;
		WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* L_35 = V_4;
		NullCheck(L_35);
		L_35->___group_7 = __this;
		Il2CppCodeGenWriteBarrier((void**)(&L_35->___group_7), (void*)__this);
		// foreach (var cell in wall.EnumerateOccupiedCells())
		WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* L_36 = V_4;
		NullCheck(L_36);
		RuntimeObject* L_37;
		L_37 = WallItem_EnumerateOccupiedCells_mA15C2F1FBAAE6D7E2F190B8396AF069D35D717BF(L_36, NULL);
		NullCheck(L_37);
		RuntimeObject* L_38;
		L_38 = InterfaceFuncInvoker0< RuntimeObject* >::Invoke(0 /* System.Collections.Generic.IEnumerator`1<T> System.Collections.Generic.IEnumerable`1<UnityEngine.Vector2Int>::GetEnumerator() */, IEnumerable_1_t8845214D7CADFAAD7AB98132A368905184A79DDF_il2cpp_TypeInfo_var, L_37);
		V_5 = L_38;
	}
	{
		auto __finallyBlock = il2cpp::utils::Finally([&]
		{

FINALLY_00f0:
			{// begin finally (depth: 1)
				{
					RuntimeObject* L_39 = V_5;
					if (!L_39)
					{
						goto IL_00fb;
					}
				}
				{
					RuntimeObject* L_40 = V_5;
					NullCheck(L_40);
					InterfaceActionInvoker0::Invoke(0 /* System.Void System.IDisposable::Dispose() */, IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5_il2cpp_TypeInfo_var, L_40);
				}

IL_00fb:
				{
					return;
				}
			}// end finally (depth: 1)
		});
		try
		{// begin try (depth: 1)
			{
				goto IL_00e5_1;
			}

IL_00ac_1:
			{
				// foreach (var cell in wall.EnumerateOccupiedCells())
				RuntimeObject* L_41 = V_5;
				NullCheck(L_41);
				Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A L_42;
				L_42 = InterfaceFuncInvoker0< Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A >::Invoke(0 /* T System.Collections.Generic.IEnumerator`1<UnityEngine.Vector2Int>::get_Current() */, IEnumerator_1_t772680255A1A75379853D3823763B88BF026E055_il2cpp_TypeInfo_var, L_41);
				V_6 = L_42;
				// if (IsInRange(cell.x, cell.y))
				int32_t L_43;
				L_43 = Vector2Int_get_x_mA2CACB1B6E6B5AD0CCC32B2CD2EDCE3ECEB50576_inline((&V_6), NULL);
				int32_t L_44;
				L_44 = Vector2Int_get_y_m48454163ECF0B463FB5A16A0C4FC4B14DB0768B3_inline((&V_6), NULL);
				bool L_45;
				L_45 = PixelGroup_IsInRange_m7D51A009EE60A9ADDC0707368ADDDE95FCF788C3(__this, L_43, L_44, NULL);
				if (!L_45)
				{
					goto IL_00e5_1;
				}
			}
			{
				// wallGrid[cell.x, cell.y] = true;
				BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* L_46 = __this->___wallGrid_17;
				int32_t L_47;
				L_47 = Vector2Int_get_x_mA2CACB1B6E6B5AD0CCC32B2CD2EDCE3ECEB50576_inline((&V_6), NULL);
				int32_t L_48;
				L_48 = Vector2Int_get_y_m48454163ECF0B463FB5A16A0C4FC4B14DB0768B3_inline((&V_6), NULL);
				NullCheck(L_46);
				(L_46)->SetAt(L_47, L_48, (bool)1);
			}

IL_00e5_1:
			{
				// foreach (var cell in wall.EnumerateOccupiedCells())
				RuntimeObject* L_49 = V_5;
				NullCheck(L_49);
				bool L_50;
				L_50 = InterfaceFuncInvoker0< bool >::Invoke(0 /* System.Boolean System.Collections.IEnumerator::MoveNext() */, IEnumerator_t7B609C2FFA6EB5167D9C62A0C32A21DE2F666DAA_il2cpp_TypeInfo_var, L_49);
				if (L_50)
				{
					goto IL_00ac_1;
				}
			}
			{
				goto IL_00fc;
			}
		}// end try (depth: 1)
		catch(Il2CppExceptionWrapper& e)
		{
			__finallyBlock.StoreException(e.ex);
		}
	}

IL_00fc:
	{
		int32_t L_51 = V_1;
		V_1 = ((int32_t)il2cpp_codegen_add(L_51, 1));
	}

IL_0100:
	{
		// foreach (var wall in GetComponentsInChildren<WallItem>())
		int32_t L_52 = V_1;
		WallItemU5BU5D_tD1B1EF0B4FAE8376B92DB7B23ACCE192653C9B58* L_53 = V_3;
		NullCheck(L_53);
		if ((((int32_t)L_52) < ((int32_t)((int32_t)(((RuntimeArray*)L_53)->max_length)))))
		{
			goto IL_0085;
		}
	}
	{
		// }
		return;
	}
}
// CrowdMatch.PixelItem CrowdMatch.PixelGroup::GetItem(System.Int32,System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* PixelGroup_GetItem_mDD0EEC5AAF354352C1CC8B66BE3C59FD78F4B8DE (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, int32_t ___col0, int32_t ___row1, const RuntimeMethod* method) 
{
	{
		// if (grid == null)
		PixelItemU5BU2CU5D_t26ACEBF9031979698453CA52C61A20C0A1A68C7F* L_0 = __this->___grid_16;
		if (L_0)
		{
			goto IL_000a;
		}
	}
	{
		// return null;
		return (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E*)NULL;
	}

IL_000a:
	{
		// if (!IsInRange(col, row))
		int32_t L_1 = ___col0;
		int32_t L_2 = ___row1;
		bool L_3;
		L_3 = PixelGroup_IsInRange_m7D51A009EE60A9ADDC0707368ADDDE95FCF788C3(__this, L_1, L_2, NULL);
		if (L_3)
		{
			goto IL_0016;
		}
	}
	{
		// return null;
		return (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E*)NULL;
	}

IL_0016:
	{
		// return grid[col, row];
		PixelItemU5BU2CU5D_t26ACEBF9031979698453CA52C61A20C0A1A68C7F* L_4 = __this->___grid_16;
		int32_t L_5 = ___col0;
		int32_t L_6 = ___row1;
		NullCheck(L_4);
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_7;
		L_7 = (L_4)->GetAt(L_5, L_6);
		return L_7;
	}
}
// System.Boolean CrowdMatch.PixelGroup::IsInRange(System.Int32,System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool PixelGroup_IsInRange_m7D51A009EE60A9ADDC0707368ADDDE95FCF788C3 (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, int32_t ___col0, int32_t ___row1, const RuntimeMethod* method) 
{
	{
		// return col >= 0 && col < columns && row >= 0 && row < TotalRows;
		int32_t L_0 = ___col0;
		if ((((int32_t)L_0) < ((int32_t)0)))
		{
			goto IL_001b;
		}
	}
	{
		int32_t L_1 = ___col0;
		int32_t L_2 = __this->___columns_7;
		if ((((int32_t)L_1) >= ((int32_t)L_2)))
		{
			goto IL_001b;
		}
	}
	{
		int32_t L_3 = ___row1;
		if ((((int32_t)L_3) < ((int32_t)0)))
		{
			goto IL_001b;
		}
	}
	{
		int32_t L_4 = ___row1;
		int32_t L_5;
		L_5 = PixelGroup_get_TotalRows_m463568B781F3699B24713B03D51CB2A5084E2D79(__this, NULL);
		return (bool)((((int32_t)L_4) < ((int32_t)L_5))? 1 : 0);
	}

IL_001b:
	{
		return (bool)0;
	}
}
// System.Boolean CrowdMatch.PixelGroup::IsWall(System.Int32,System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool PixelGroup_IsWall_m2680CC2BF1BAB1591F9AC9C7D56E714047AE9B87 (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, int32_t ___col0, int32_t ___row1, const RuntimeMethod* method) 
{
	{
		// if (wallGrid == null)
		BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* L_0 = __this->___wallGrid_17;
		if (L_0)
		{
			goto IL_000a;
		}
	}
	{
		// return false;
		return (bool)0;
	}

IL_000a:
	{
		// if (!IsInRange(col, row))
		int32_t L_1 = ___col0;
		int32_t L_2 = ___row1;
		bool L_3;
		L_3 = PixelGroup_IsInRange_m7D51A009EE60A9ADDC0707368ADDDE95FCF788C3(__this, L_1, L_2, NULL);
		if (L_3)
		{
			goto IL_0016;
		}
	}
	{
		// return false;
		return (bool)0;
	}

IL_0016:
	{
		// return wallGrid[col, row];
		BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* L_4 = __this->___wallGrid_17;
		int32_t L_5 = ___col0;
		int32_t L_6 = ___row1;
		NullCheck(L_4);
		bool L_7;
		L_7 = (L_4)->GetAt(L_5, L_6);
		return L_7;
	}
}
// System.Boolean CrowdMatch.PixelGroup::IsEmpty(System.Int32,System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool PixelGroup_IsEmpty_m67995CF792BAD887755E45E0F0D2FEDECDE3F570 (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, int32_t ___col0, int32_t ___row1, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		// if (!IsInRange(col, row))
		int32_t L_0 = ___col0;
		int32_t L_1 = ___row1;
		bool L_2;
		L_2 = PixelGroup_IsInRange_m7D51A009EE60A9ADDC0707368ADDDE95FCF788C3(__this, L_0, L_1, NULL);
		if (L_2)
		{
			goto IL_000c;
		}
	}
	{
		// return false;
		return (bool)0;
	}

IL_000c:
	{
		// if (grid == null)
		PixelItemU5BU2CU5D_t26ACEBF9031979698453CA52C61A20C0A1A68C7F* L_3 = __this->___grid_16;
		if (L_3)
		{
			goto IL_0016;
		}
	}
	{
		// return false;
		return (bool)0;
	}

IL_0016:
	{
		// return grid[col, row] == null && !IsWall(col, row);
		PixelItemU5BU2CU5D_t26ACEBF9031979698453CA52C61A20C0A1A68C7F* L_4 = __this->___grid_16;
		int32_t L_5 = ___col0;
		int32_t L_6 = ___row1;
		NullCheck(L_4);
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_7;
		L_7 = (L_4)->GetAt(L_5, L_6);
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_8;
		L_8 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_7, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_8)
		{
			goto IL_0037;
		}
	}
	{
		int32_t L_9 = ___col0;
		int32_t L_10 = ___row1;
		bool L_11;
		L_11 = PixelGroup_IsWall_m2680CC2BF1BAB1591F9AC9C7D56E714047AE9B87(__this, L_9, L_10, NULL);
		return (bool)((((int32_t)L_11) == ((int32_t)0))? 1 : 0);
	}

IL_0037:
	{
		return (bool)0;
	}
}
// UnityEngine.Vector3 CrowdMatch.PixelGroup::GetLocalPosition(System.Int32,System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 PixelGroup_GetLocalPosition_mA20E329BB838AB5B672C2A969C0E5B52A1771DD3 (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, int32_t ___col0, int32_t ___row1, const RuntimeMethod* method) 
{
	float V_0 = 0.0f;
	{
		// float x = (col - (columns - 1) * 0.5f) * CellSizeX;
		int32_t L_0 = ___col0;
		int32_t L_1 = __this->___columns_7;
		float L_2;
		L_2 = PixelGroup_get_CellSizeX_mFF4EC1FE8D75565520456EB7BD12D9DFCF2AFAE0(__this, NULL);
		// float z = -row * CellSizeZ;
		int32_t L_3 = ___row1;
		float L_4;
		L_4 = PixelGroup_get_CellSizeZ_m8BD98A189CFE63A89D5928CDE4EB4DD7D681CC50(__this, NULL);
		V_0 = ((float)il2cpp_codegen_multiply(((float)((-L_3))), L_4));
		// return new Vector3(x, 0f, z);
		float L_5 = V_0;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_6;
		memset((&L_6), 0, sizeof(L_6));
		Vector3__ctor_m376936E6B999EF1ECBE57D990A386303E2283DE0_inline((&L_6), ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_subtract(((float)L_0), ((float)il2cpp_codegen_multiply(((float)((int32_t)il2cpp_codegen_subtract(L_1, 1))), (0.5f))))), L_2)), (0.0f), L_5, /*hidden argument*/NULL);
		return L_6;
	}
}
// UnityEngine.Vector3 CrowdMatch.PixelGroup::GetWorldPosition(System.Int32,System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 PixelGroup_GetWorldPosition_m53DF201A615D1467AB8512D30C752A3826FBE608 (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, int32_t ___col0, int32_t ___row1, const RuntimeMethod* method) 
{
	{
		// return transform.TransformPoint(GetLocalPosition(col, row));
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_0;
		L_0 = Component_get_transform_m2919A1D81931E6932C7F06D4C2F0AB8DDA9A5371(__this, NULL);
		int32_t L_1 = ___col0;
		int32_t L_2 = ___row1;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_3;
		L_3 = PixelGroup_GetLocalPosition_mA20E329BB838AB5B672C2A969C0E5B52A1771DD3(__this, L_1, L_2, NULL);
		NullCheck(L_0);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_4;
		L_4 = Transform_TransformPoint_m05BFF013DB830D7BFE44A007703694AE1062EE44(L_0, L_3, NULL);
		return L_4;
	}
}
// System.Void CrowdMatch.PixelGroup::RefreshExposed()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelGroup_RefreshExposed_mB1ABCFD73B4901915B5669FE0721EF544B489C16 (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Enumerator_Dispose_m2A96F62698864FA1E73292450EBF2019F7104BBC_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Enumerator_MoveNext_mD6D16710D40F62D081A4973E4D8CA1614D1482B5_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Enumerator_get_Current_m87245A61572727EBCD1642C4A2BD99B11CE9FA8A_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_Add_m771AC7A01DFC931CCCFCCF949C1F4D56B5E98A1B_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_GetEnumerator_m039302BD172C3288503DB73B6E2B27B8D8BC0321_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1__ctor_mE1D9FD9DA1EF2CAC4F99EF4E013F05BB8C3507EF_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_tB56F1028A724D2CE4E84861619D1CF68C68C983D_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Queue_1_Dequeue_mD30377AD154F6A542F280578B393102A1D5378EB_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Queue_1_Enqueue_m8507576A07092ADD61289B4C7F248A6C94944EFB_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Queue_1__ctor_m262194FC7D76E5DB95E022130A30E64125C0A90E_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Queue_1_get_Count_m1FE2DD00C23DF83E6DBC5BE6A23A9FE7FBF772DD_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Queue_1_t9B96DF949ACABD4B0F595738FC87DC80BCCC3875_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	int32_t V_1 = 0;
	BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* V_2 = NULL;
	BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* V_3 = NULL;
	BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* V_4 = NULL;
	Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* V_5 = NULL;
	Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* V_6 = NULL;
	int32_t V_7 = 0;
	int32_t V_8 = 0;
	int32_t V_9 = 0;
	int32_t V_10 = 0;
	int32_t V_11 = 0;
	List_1_tB56F1028A724D2CE4E84861619D1CF68C68C983D* V_12 = NULL;
	bool V_13 = false;
	Queue_1_t9B96DF949ACABD4B0F595738FC87DC80BCCC3875* V_14 = NULL;
	Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A V_15;
	memset((&V_15), 0, sizeof(V_15));
	int32_t V_16 = 0;
	int32_t V_17 = 0;
	int32_t V_18 = 0;
	PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* V_19 = NULL;
	Enumerator_t4BD1B5394A2E9960562CC364C2CCE4FCBD1AC258 V_20;
	memset((&V_20), 0, sizeof(V_20));
	Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A V_21;
	memset((&V_21), 0, sizeof(V_21));
	int32_t V_22 = 0;
	int32_t V_23 = 0;
	PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* V_24 = NULL;
	int32_t G_B15_0 = 0;
	int32_t G_B15_1 = 0;
	BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* G_B15_2 = NULL;
	int32_t G_B7_0 = 0;
	int32_t G_B7_1 = 0;
	BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* G_B7_2 = NULL;
	int32_t G_B8_0 = 0;
	int32_t G_B8_1 = 0;
	BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* G_B8_2 = NULL;
	int32_t G_B10_0 = 0;
	int32_t G_B10_1 = 0;
	BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* G_B10_2 = NULL;
	int32_t G_B9_0 = 0;
	int32_t G_B9_1 = 0;
	BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* G_B9_2 = NULL;
	int32_t G_B12_0 = 0;
	int32_t G_B12_1 = 0;
	BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* G_B12_2 = NULL;
	int32_t G_B11_0 = 0;
	int32_t G_B11_1 = 0;
	BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* G_B11_2 = NULL;
	int32_t G_B14_0 = 0;
	int32_t G_B14_1 = 0;
	BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* G_B14_2 = NULL;
	int32_t G_B13_0 = 0;
	int32_t G_B13_1 = 0;
	BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* G_B13_2 = NULL;
	int32_t G_B16_0 = 0;
	int32_t G_B16_1 = 0;
	int32_t G_B16_2 = 0;
	BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* G_B16_3 = NULL;
	{
		// if (grid == null)
		PixelItemU5BU2CU5D_t26ACEBF9031979698453CA52C61A20C0A1A68C7F* L_0 = __this->___grid_16;
		if (L_0)
		{
			goto IL_000e;
		}
	}
	{
		// RebuildGrid();
		PixelGroup_RebuildGrid_mDE9E7C6B2933B5C2FDBCE71287AA8ECC2D2510E1(__this, NULL);
	}

IL_000e:
	{
		// int cols = columns;
		int32_t L_1 = __this->___columns_7;
		V_0 = L_1;
		// int totalRows = TotalRows;
		int32_t L_2;
		L_2 = PixelGroup_get_TotalRows_m463568B781F3699B24713B03D51CB2A5084E2D79(__this, NULL);
		V_1 = L_2;
		// var directlyExposed = new bool[cols, totalRows];
		int32_t L_3 = V_0;
		int32_t L_4 = V_1;
		il2cpp_array_size_t L_6[] = { (il2cpp_array_size_t)L_3, (il2cpp_array_size_t)L_4 };
		BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* L_5 = (BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6*)GenArrayNew(BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6_il2cpp_TypeInfo_var, L_6);
		V_2 = L_5;
		// for (int c = 0; c < cols; c++)
		V_7 = 0;
		goto IL_00ca;
	}

IL_002c:
	{
		// for (int r = 0; r < totalRows; r++)
		V_8 = 0;
		goto IL_00bc;
	}

IL_0034:
	{
		// if (grid[c, r] == null || IsWall(c, r))
		PixelItemU5BU2CU5D_t26ACEBF9031979698453CA52C61A20C0A1A68C7F* L_7 = __this->___grid_16;
		int32_t L_8 = V_7;
		int32_t L_9 = V_8;
		NullCheck(L_7);
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_10;
		L_10 = (L_7)->GetAt(L_8, L_9);
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_11;
		L_11 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_10, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (L_11)
		{
			goto IL_00b6;
		}
	}
	{
		int32_t L_12 = V_7;
		int32_t L_13 = V_8;
		bool L_14;
		L_14 = PixelGroup_IsWall_m2680CC2BF1BAB1591F9AC9C7D56E714047AE9B87(__this, L_12, L_13, NULL);
		if (L_14)
		{
			goto IL_00b6;
		}
	}
	{
		// directlyExposed[c, r] =
		//     r == 0 ||                                            // ??????????
		//     IsEmpty(c, r - 1) ||                                 // ???
		//     (r + 1 < totalRows && IsEmpty(c, r + 1)) ||          // ?????????
		//     (c - 1 >= 0 && IsEmpty(c - 1, r)) ||                 // ?????????
		//     (c + 1 < cols && IsEmpty(c + 1, r));                 // ?????????
		BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* L_15 = V_2;
		int32_t L_16 = V_7;
		int32_t L_17 = V_8;
		int32_t L_18 = V_8;
		G_B7_0 = L_17;
		G_B7_1 = L_16;
		G_B7_2 = L_15;
		if (!L_18)
		{
			G_B15_0 = L_17;
			G_B15_1 = L_16;
			G_B15_2 = L_15;
			goto IL_00b0;
		}
	}
	{
		int32_t L_19 = V_7;
		int32_t L_20 = V_8;
		bool L_21;
		L_21 = PixelGroup_IsEmpty_m67995CF792BAD887755E45E0F0D2FEDECDE3F570(__this, L_19, ((int32_t)il2cpp_codegen_subtract(L_20, 1)), NULL);
		G_B8_0 = G_B7_0;
		G_B8_1 = G_B7_1;
		G_B8_2 = G_B7_2;
		if (L_21)
		{
			G_B15_0 = G_B7_0;
			G_B15_1 = G_B7_1;
			G_B15_2 = G_B7_2;
			goto IL_00b0;
		}
	}
	{
		int32_t L_22 = V_8;
		int32_t L_23 = V_1;
		G_B9_0 = G_B8_0;
		G_B9_1 = G_B8_1;
		G_B9_2 = G_B8_2;
		if ((((int32_t)((int32_t)il2cpp_codegen_add(L_22, 1))) >= ((int32_t)L_23)))
		{
			G_B10_0 = G_B8_0;
			G_B10_1 = G_B8_1;
			G_B10_2 = G_B8_2;
			goto IL_0083;
		}
	}
	{
		int32_t L_24 = V_7;
		int32_t L_25 = V_8;
		bool L_26;
		L_26 = PixelGroup_IsEmpty_m67995CF792BAD887755E45E0F0D2FEDECDE3F570(__this, L_24, ((int32_t)il2cpp_codegen_add(L_25, 1)), NULL);
		G_B10_0 = G_B9_0;
		G_B10_1 = G_B9_1;
		G_B10_2 = G_B9_2;
		if (L_26)
		{
			G_B15_0 = G_B9_0;
			G_B15_1 = G_B9_1;
			G_B15_2 = G_B9_2;
			goto IL_00b0;
		}
	}

IL_0083:
	{
		int32_t L_27 = V_7;
		G_B11_0 = G_B10_0;
		G_B11_1 = G_B10_1;
		G_B11_2 = G_B10_2;
		if ((((int32_t)((int32_t)il2cpp_codegen_subtract(L_27, 1))) < ((int32_t)0)))
		{
			G_B12_0 = G_B10_0;
			G_B12_1 = G_B10_1;
			G_B12_2 = G_B10_2;
			goto IL_0098;
		}
	}
	{
		int32_t L_28 = V_7;
		int32_t L_29 = V_8;
		bool L_30;
		L_30 = PixelGroup_IsEmpty_m67995CF792BAD887755E45E0F0D2FEDECDE3F570(__this, ((int32_t)il2cpp_codegen_subtract(L_28, 1)), L_29, NULL);
		G_B12_0 = G_B11_0;
		G_B12_1 = G_B11_1;
		G_B12_2 = G_B11_2;
		if (L_30)
		{
			G_B15_0 = G_B11_0;
			G_B15_1 = G_B11_1;
			G_B15_2 = G_B11_2;
			goto IL_00b0;
		}
	}

IL_0098:
	{
		int32_t L_31 = V_7;
		int32_t L_32 = V_0;
		G_B13_0 = G_B12_0;
		G_B13_1 = G_B12_1;
		G_B13_2 = G_B12_2;
		if ((((int32_t)((int32_t)il2cpp_codegen_add(L_31, 1))) >= ((int32_t)L_32)))
		{
			G_B14_0 = G_B12_0;
			G_B14_1 = G_B12_1;
			G_B14_2 = G_B12_2;
			goto IL_00ad;
		}
	}
	{
		int32_t L_33 = V_7;
		int32_t L_34 = V_8;
		bool L_35;
		L_35 = PixelGroup_IsEmpty_m67995CF792BAD887755E45E0F0D2FEDECDE3F570(__this, ((int32_t)il2cpp_codegen_add(L_33, 1)), L_34, NULL);
		G_B16_0 = ((int32_t)(L_35));
		G_B16_1 = G_B13_0;
		G_B16_2 = G_B13_1;
		G_B16_3 = G_B13_2;
		goto IL_00b1;
	}

IL_00ad:
	{
		G_B16_0 = 0;
		G_B16_1 = G_B14_0;
		G_B16_2 = G_B14_1;
		G_B16_3 = G_B14_2;
		goto IL_00b1;
	}

IL_00b0:
	{
		G_B16_0 = 1;
		G_B16_1 = G_B15_0;
		G_B16_2 = G_B15_1;
		G_B16_3 = G_B15_2;
	}

IL_00b1:
	{
		NullCheck(G_B16_3);
		(G_B16_3)->SetAt(G_B16_2, G_B16_1, (bool)G_B16_0);
	}

IL_00b6:
	{
		// for (int r = 0; r < totalRows; r++)
		int32_t L_36 = V_8;
		V_8 = ((int32_t)il2cpp_codegen_add(L_36, 1));
	}

IL_00bc:
	{
		// for (int r = 0; r < totalRows; r++)
		int32_t L_37 = V_8;
		int32_t L_38 = V_1;
		if ((((int32_t)L_37) < ((int32_t)L_38)))
		{
			goto IL_0034;
		}
	}
	{
		// for (int c = 0; c < cols; c++)
		int32_t L_39 = V_7;
		V_7 = ((int32_t)il2cpp_codegen_add(L_39, 1));
	}

IL_00ca:
	{
		// for (int c = 0; c < cols; c++)
		int32_t L_40 = V_7;
		int32_t L_41 = V_0;
		if ((((int32_t)L_40) < ((int32_t)L_41)))
		{
			goto IL_002c;
		}
	}
	{
		// var visited = new bool[cols, totalRows];
		int32_t L_42 = V_0;
		int32_t L_43 = V_1;
		il2cpp_array_size_t L_45[] = { (il2cpp_array_size_t)L_42, (il2cpp_array_size_t)L_43 };
		BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* L_44 = (BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6*)GenArrayNew(BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6_il2cpp_TypeInfo_var, L_45);
		V_3 = L_44;
		// var active = new bool[cols, totalRows];
		int32_t L_46 = V_0;
		int32_t L_47 = V_1;
		il2cpp_array_size_t L_49[] = { (il2cpp_array_size_t)L_46, (il2cpp_array_size_t)L_47 };
		BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* L_48 = (BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6*)GenArrayNew(BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6_il2cpp_TypeInfo_var, L_49);
		V_4 = L_48;
		// int[] dx = { 1, -1, 0, 0 };
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_50 = (Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C*)(Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C*)SZArrayNew(Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C_il2cpp_TypeInfo_var, (uint32_t)4);
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_51 = L_50;
		NullCheck(L_51);
		(L_51)->SetAt(static_cast<il2cpp_array_size_t>(0), (int32_t)1);
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_52 = L_51;
		NullCheck(L_52);
		(L_52)->SetAt(static_cast<il2cpp_array_size_t>(1), (int32_t)(-1));
		V_5 = L_52;
		// int[] dz = { 0, 0, 1, -1 };
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_53 = (Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C*)(Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C*)SZArrayNew(Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C_il2cpp_TypeInfo_var, (uint32_t)4);
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_54 = L_53;
		NullCheck(L_54);
		(L_54)->SetAt(static_cast<il2cpp_array_size_t>(2), (int32_t)1);
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_55 = L_54;
		NullCheck(L_55);
		(L_55)->SetAt(static_cast<il2cpp_array_size_t>(3), (int32_t)(-1));
		V_6 = L_55;
		// for (int c = 0; c < cols; c++)
		V_9 = 0;
		goto IL_02c6;
	}

IL_010b:
	{
		// for (int r = 0; r < totalRows; r++)
		V_10 = 0;
		goto IL_02b8;
	}

IL_0113:
	{
		// if (grid[c, r] == null || IsWall(c, r) || visited[c, r])
		PixelItemU5BU2CU5D_t26ACEBF9031979698453CA52C61A20C0A1A68C7F* L_56 = __this->___grid_16;
		int32_t L_57 = V_9;
		int32_t L_58 = V_10;
		NullCheck(L_56);
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_59;
		L_59 = (L_56)->GetAt(L_57, L_58);
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_60;
		L_60 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_59, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (L_60)
		{
			goto IL_02b2;
		}
	}
	{
		int32_t L_61 = V_9;
		int32_t L_62 = V_10;
		bool L_63;
		L_63 = PixelGroup_IsWall_m2680CC2BF1BAB1591F9AC9C7D56E714047AE9B87(__this, L_61, L_62, NULL);
		if (L_63)
		{
			goto IL_02b2;
		}
	}
	{
		BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* L_64 = V_3;
		int32_t L_65 = V_9;
		int32_t L_66 = V_10;
		NullCheck(L_64);
		bool L_67;
		L_67 = (L_64)->GetAt(L_65, L_66);
		if (L_67)
		{
			goto IL_02b2;
		}
	}
	{
		// int color = grid[c, r].colorId;
		PixelItemU5BU2CU5D_t26ACEBF9031979698453CA52C61A20C0A1A68C7F* L_68 = __this->___grid_16;
		int32_t L_69 = V_9;
		int32_t L_70 = V_10;
		NullCheck(L_68);
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_71;
		L_71 = (L_68)->GetAt(L_69, L_70);
		NullCheck(L_71);
		int32_t L_72 = L_71->___colorId_4;
		V_11 = L_72;
		// var cells = new List<Vector2Int>();
		List_1_tB56F1028A724D2CE4E84861619D1CF68C68C983D* L_73 = (List_1_tB56F1028A724D2CE4E84861619D1CF68C68C983D*)il2cpp_codegen_object_new(List_1_tB56F1028A724D2CE4E84861619D1CF68C68C983D_il2cpp_TypeInfo_var);
		NullCheck(L_73);
		List_1__ctor_mE1D9FD9DA1EF2CAC4F99EF4E013F05BB8C3507EF(L_73, List_1__ctor_mE1D9FD9DA1EF2CAC4F99EF4E013F05BB8C3507EF_RuntimeMethod_var);
		V_12 = L_73;
		// bool hasExposed = false;
		V_13 = (bool)0;
		// var queue = new Queue<Vector2Int>();
		Queue_1_t9B96DF949ACABD4B0F595738FC87DC80BCCC3875* L_74 = (Queue_1_t9B96DF949ACABD4B0F595738FC87DC80BCCC3875*)il2cpp_codegen_object_new(Queue_1_t9B96DF949ACABD4B0F595738FC87DC80BCCC3875_il2cpp_TypeInfo_var);
		NullCheck(L_74);
		Queue_1__ctor_m262194FC7D76E5DB95E022130A30E64125C0A90E(L_74, Queue_1__ctor_m262194FC7D76E5DB95E022130A30E64125C0A90E_RuntimeMethod_var);
		V_14 = L_74;
		// queue.Enqueue(new Vector2Int(c, r));
		Queue_1_t9B96DF949ACABD4B0F595738FC87DC80BCCC3875* L_75 = V_14;
		int32_t L_76 = V_9;
		int32_t L_77 = V_10;
		Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A L_78;
		memset((&L_78), 0, sizeof(L_78));
		Vector2Int__ctor_mC20D1312133EB8CB63EC11067088B043660F11CE_inline((&L_78), L_76, L_77, /*hidden argument*/NULL);
		NullCheck(L_75);
		Queue_1_Enqueue_m8507576A07092ADD61289B4C7F248A6C94944EFB(L_75, L_78, Queue_1_Enqueue_m8507576A07092ADD61289B4C7F248A6C94944EFB_RuntimeMethod_var);
		// visited[c, r] = true;
		BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* L_79 = V_3;
		int32_t L_80 = V_9;
		int32_t L_81 = V_10;
		NullCheck(L_79);
		(L_79)->SetAt(L_80, L_81, (bool)1);
		goto IL_025e;
	}

IL_0192:
	{
		// var cur = queue.Dequeue();
		Queue_1_t9B96DF949ACABD4B0F595738FC87DC80BCCC3875* L_82 = V_14;
		NullCheck(L_82);
		Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A L_83;
		L_83 = Queue_1_Dequeue_mD30377AD154F6A542F280578B393102A1D5378EB(L_82, Queue_1_Dequeue_mD30377AD154F6A542F280578B393102A1D5378EB_RuntimeMethod_var);
		V_15 = L_83;
		// cells.Add(cur);
		List_1_tB56F1028A724D2CE4E84861619D1CF68C68C983D* L_84 = V_12;
		Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A L_85 = V_15;
		NullCheck(L_84);
		List_1_Add_m771AC7A01DFC931CCCFCCF949C1F4D56B5E98A1B_inline(L_84, L_85, List_1_Add_m771AC7A01DFC931CCCFCCF949C1F4D56B5E98A1B_RuntimeMethod_var);
		// if (directlyExposed[cur.x, cur.y])
		BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* L_86 = V_2;
		int32_t L_87;
		L_87 = Vector2Int_get_x_mA2CACB1B6E6B5AD0CCC32B2CD2EDCE3ECEB50576_inline((&V_15), NULL);
		int32_t L_88;
		L_88 = Vector2Int_get_y_m48454163ECF0B463FB5A16A0C4FC4B14DB0768B3_inline((&V_15), NULL);
		NullCheck(L_86);
		bool L_89;
		L_89 = (L_86)->GetAt(L_87, L_88);
		if (!L_89)
		{
			goto IL_01bd;
		}
	}
	{
		// hasExposed = true;
		V_13 = (bool)1;
	}

IL_01bd:
	{
		// for (int d = 0; d < 4; d++)
		V_16 = 0;
		goto IL_0256;
	}

IL_01c5:
	{
		// int nx = cur.x + dx[d];
		int32_t L_90;
		L_90 = Vector2Int_get_x_mA2CACB1B6E6B5AD0CCC32B2CD2EDCE3ECEB50576_inline((&V_15), NULL);
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_91 = V_5;
		int32_t L_92 = V_16;
		NullCheck(L_91);
		int32_t L_93 = L_92;
		int32_t L_94 = (L_91)->GetAt(static_cast<il2cpp_array_size_t>(L_93));
		V_17 = ((int32_t)il2cpp_codegen_add(L_90, L_94));
		// int nz = cur.y + dz[d];
		int32_t L_95;
		L_95 = Vector2Int_get_y_m48454163ECF0B463FB5A16A0C4FC4B14DB0768B3_inline((&V_15), NULL);
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_96 = V_6;
		int32_t L_97 = V_16;
		NullCheck(L_96);
		int32_t L_98 = L_97;
		int32_t L_99 = (L_96)->GetAt(static_cast<il2cpp_array_size_t>(L_98));
		V_18 = ((int32_t)il2cpp_codegen_add(L_95, L_99));
		// if (nx < 0 || nx >= cols || nz < 0 || nz >= totalRows)
		int32_t L_100 = V_17;
		if ((((int32_t)L_100) < ((int32_t)0)))
		{
			goto IL_0250;
		}
	}
	{
		int32_t L_101 = V_17;
		int32_t L_102 = V_0;
		if ((((int32_t)L_101) >= ((int32_t)L_102)))
		{
			goto IL_0250;
		}
	}
	{
		int32_t L_103 = V_18;
		if ((((int32_t)L_103) < ((int32_t)0)))
		{
			goto IL_0250;
		}
	}
	{
		int32_t L_104 = V_18;
		int32_t L_105 = V_1;
		if ((((int32_t)L_104) >= ((int32_t)L_105)))
		{
			goto IL_0250;
		}
	}
	{
		// if (visited[nx, nz])
		BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* L_106 = V_3;
		int32_t L_107 = V_17;
		int32_t L_108 = V_18;
		NullCheck(L_106);
		bool L_109;
		L_109 = (L_106)->GetAt(L_107, L_108);
		if (L_109)
		{
			goto IL_0250;
		}
	}
	{
		// var nb = grid[nx, nz];
		PixelItemU5BU2CU5D_t26ACEBF9031979698453CA52C61A20C0A1A68C7F* L_110 = __this->___grid_16;
		int32_t L_111 = V_17;
		int32_t L_112 = V_18;
		NullCheck(L_110);
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_113;
		L_113 = (L_110)->GetAt(L_111, L_112);
		V_19 = L_113;
		// if (nb == null || IsWall(nx, nz) || nb.colorId != color)
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_114 = V_19;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_115;
		L_115 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_114, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (L_115)
		{
			goto IL_0250;
		}
	}
	{
		int32_t L_116 = V_17;
		int32_t L_117 = V_18;
		bool L_118;
		L_118 = PixelGroup_IsWall_m2680CC2BF1BAB1591F9AC9C7D56E714047AE9B87(__this, L_116, L_117, NULL);
		if (L_118)
		{
			goto IL_0250;
		}
	}
	{
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_119 = V_19;
		NullCheck(L_119);
		int32_t L_120 = L_119->___colorId_4;
		int32_t L_121 = V_11;
		if ((!(((uint32_t)L_120) == ((uint32_t)L_121))))
		{
			goto IL_0250;
		}
	}
	{
		// visited[nx, nz] = true;
		BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* L_122 = V_3;
		int32_t L_123 = V_17;
		int32_t L_124 = V_18;
		NullCheck(L_122);
		(L_122)->SetAt(L_123, L_124, (bool)1);
		// queue.Enqueue(new Vector2Int(nx, nz));
		Queue_1_t9B96DF949ACABD4B0F595738FC87DC80BCCC3875* L_125 = V_14;
		int32_t L_126 = V_17;
		int32_t L_127 = V_18;
		Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A L_128;
		memset((&L_128), 0, sizeof(L_128));
		Vector2Int__ctor_mC20D1312133EB8CB63EC11067088B043660F11CE_inline((&L_128), L_126, L_127, /*hidden argument*/NULL);
		NullCheck(L_125);
		Queue_1_Enqueue_m8507576A07092ADD61289B4C7F248A6C94944EFB(L_125, L_128, Queue_1_Enqueue_m8507576A07092ADD61289B4C7F248A6C94944EFB_RuntimeMethod_var);
	}

IL_0250:
	{
		// for (int d = 0; d < 4; d++)
		int32_t L_129 = V_16;
		V_16 = ((int32_t)il2cpp_codegen_add(L_129, 1));
	}

IL_0256:
	{
		// for (int d = 0; d < 4; d++)
		int32_t L_130 = V_16;
		if ((((int32_t)L_130) < ((int32_t)4)))
		{
			goto IL_01c5;
		}
	}

IL_025e:
	{
		// while (queue.Count > 0)
		Queue_1_t9B96DF949ACABD4B0F595738FC87DC80BCCC3875* L_131 = V_14;
		NullCheck(L_131);
		int32_t L_132;
		L_132 = Queue_1_get_Count_m1FE2DD00C23DF83E6DBC5BE6A23A9FE7FBF772DD_inline(L_131, Queue_1_get_Count_m1FE2DD00C23DF83E6DBC5BE6A23A9FE7FBF772DD_RuntimeMethod_var);
		if ((((int32_t)L_132) > ((int32_t)0)))
		{
			goto IL_0192;
		}
	}
	{
		// if (!hasExposed)
		bool L_133 = V_13;
		if (!L_133)
		{
			goto IL_02b2;
		}
	}
	{
		// foreach (var cell in cells)
		List_1_tB56F1028A724D2CE4E84861619D1CF68C68C983D* L_134 = V_12;
		NullCheck(L_134);
		Enumerator_t4BD1B5394A2E9960562CC364C2CCE4FCBD1AC258 L_135;
		L_135 = List_1_GetEnumerator_m039302BD172C3288503DB73B6E2B27B8D8BC0321(L_134, List_1_GetEnumerator_m039302BD172C3288503DB73B6E2B27B8D8BC0321_RuntimeMethod_var);
		V_20 = L_135;
	}
	{
		auto __finallyBlock = il2cpp::utils::Finally([&]
		{

FINALLY_02a4:
			{// begin finally (depth: 1)
				Enumerator_Dispose_m2A96F62698864FA1E73292450EBF2019F7104BBC((&V_20), Enumerator_Dispose_m2A96F62698864FA1E73292450EBF2019F7104BBC_RuntimeMethod_var);
				return;
			}// end finally (depth: 1)
		});
		try
		{// begin try (depth: 1)
			{
				goto IL_0299_1;
			}

IL_027a_1:
			{
				// foreach (var cell in cells)
				Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A L_136;
				L_136 = Enumerator_get_Current_m87245A61572727EBCD1642C4A2BD99B11CE9FA8A_inline((&V_20), Enumerator_get_Current_m87245A61572727EBCD1642C4A2BD99B11CE9FA8A_RuntimeMethod_var);
				V_21 = L_136;
				// active[cell.x, cell.y] = true;
				BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* L_137 = V_4;
				int32_t L_138;
				L_138 = Vector2Int_get_x_mA2CACB1B6E6B5AD0CCC32B2CD2EDCE3ECEB50576_inline((&V_21), NULL);
				int32_t L_139;
				L_139 = Vector2Int_get_y_m48454163ECF0B463FB5A16A0C4FC4B14DB0768B3_inline((&V_21), NULL);
				NullCheck(L_137);
				(L_137)->SetAt(L_138, L_139, (bool)1);
			}

IL_0299_1:
			{
				// foreach (var cell in cells)
				bool L_140;
				L_140 = Enumerator_MoveNext_mD6D16710D40F62D081A4973E4D8CA1614D1482B5((&V_20), Enumerator_MoveNext_mD6D16710D40F62D081A4973E4D8CA1614D1482B5_RuntimeMethod_var);
				if (L_140)
				{
					goto IL_027a_1;
				}
			}
			{
				goto IL_02b2;
			}
		}// end try (depth: 1)
		catch(Il2CppExceptionWrapper& e)
		{
			__finallyBlock.StoreException(e.ex);
		}
	}

IL_02b2:
	{
		// for (int r = 0; r < totalRows; r++)
		int32_t L_141 = V_10;
		V_10 = ((int32_t)il2cpp_codegen_add(L_141, 1));
	}

IL_02b8:
	{
		// for (int r = 0; r < totalRows; r++)
		int32_t L_142 = V_10;
		int32_t L_143 = V_1;
		if ((((int32_t)L_142) < ((int32_t)L_143)))
		{
			goto IL_0113;
		}
	}
	{
		// for (int c = 0; c < cols; c++)
		int32_t L_144 = V_9;
		V_9 = ((int32_t)il2cpp_codegen_add(L_144, 1));
	}

IL_02c6:
	{
		// for (int c = 0; c < cols; c++)
		int32_t L_145 = V_9;
		int32_t L_146 = V_0;
		if ((((int32_t)L_145) < ((int32_t)L_146)))
		{
			goto IL_010b;
		}
	}
	{
		// for (int c = 0; c < cols; c++)
		V_22 = 0;
		goto IL_0316;
	}

IL_02d3:
	{
		// for (int r = 0; r < totalRows; r++)
		V_23 = 0;
		goto IL_030b;
	}

IL_02d8:
	{
		// var item = grid[c, r];
		PixelItemU5BU2CU5D_t26ACEBF9031979698453CA52C61A20C0A1A68C7F* L_147 = __this->___grid_16;
		int32_t L_148 = V_22;
		int32_t L_149 = V_23;
		NullCheck(L_147);
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_150;
		L_150 = (L_147)->GetAt(L_148, L_149);
		V_24 = L_150;
		// if (item == null)
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_151 = V_24;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_152;
		L_152 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_151, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (L_152)
		{
			goto IL_0305;
		}
	}
	{
		// item.SetExposed(active[c, r]);
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_153 = V_24;
		BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* L_154 = V_4;
		int32_t L_155 = V_22;
		int32_t L_156 = V_23;
		NullCheck(L_154);
		bool L_157;
		L_157 = (L_154)->GetAt(L_155, L_156);
		NullCheck(L_153);
		PixelItem_SetExposed_mBDE7ACCCFA45DD1DE0B8271008CA72B124A07D95(L_153, L_157, NULL);
	}

IL_0305:
	{
		// for (int r = 0; r < totalRows; r++)
		int32_t L_158 = V_23;
		V_23 = ((int32_t)il2cpp_codegen_add(L_158, 1));
	}

IL_030b:
	{
		// for (int r = 0; r < totalRows; r++)
		int32_t L_159 = V_23;
		int32_t L_160 = V_1;
		if ((((int32_t)L_159) < ((int32_t)L_160)))
		{
			goto IL_02d8;
		}
	}
	{
		// for (int c = 0; c < cols; c++)
		int32_t L_161 = V_22;
		V_22 = ((int32_t)il2cpp_codegen_add(L_161, 1));
	}

IL_0316:
	{
		// for (int c = 0; c < cols; c++)
		int32_t L_162 = V_22;
		int32_t L_163 = V_0;
		if ((((int32_t)L_162) < ((int32_t)L_163)))
		{
			goto IL_02d3;
		}
	}
	{
		// }
		return;
	}
}
// System.Void CrowdMatch.PixelGroup::ClearPixels()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelGroup_ClearPixels_m8AF2B7BEB2B33BB0F3AFFE106F5EDE2DE71CC607 (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Component_GetComponentsInChildren_TisPixelItem_t863890C77945A8C08435BF2F8859A47A413C794E_mC6DFA6AEA610A79B3113B66C8457AA3325D29586_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	PixelItemU5BU5D_t48D4473FA8D417437EB43513F73EC0FD24CBD02F* V_0 = NULL;
	int32_t V_1 = 0;
	PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* V_2 = NULL;
	{
		// var items = GetComponentsInChildren<PixelItem>();
		PixelItemU5BU5D_t48D4473FA8D417437EB43513F73EC0FD24CBD02F* L_0;
		L_0 = Component_GetComponentsInChildren_TisPixelItem_t863890C77945A8C08435BF2F8859A47A413C794E_mC6DFA6AEA610A79B3113B66C8457AA3325D29586(__this, Component_GetComponentsInChildren_TisPixelItem_t863890C77945A8C08435BF2F8859A47A413C794E_mC6DFA6AEA610A79B3113B66C8457AA3325D29586_RuntimeMethod_var);
		V_0 = L_0;
		// for (int i = items.Length - 1; i >= 0; i--)
		PixelItemU5BU5D_t48D4473FA8D417437EB43513F73EC0FD24CBD02F* L_1 = V_0;
		NullCheck(L_1);
		V_1 = ((int32_t)il2cpp_codegen_subtract(((int32_t)(((RuntimeArray*)L_1)->max_length)), 1));
		goto IL_004c;
	}

IL_000f:
	{
		// var it = items[i];
		PixelItemU5BU5D_t48D4473FA8D417437EB43513F73EC0FD24CBD02F* L_2 = V_0;
		int32_t L_3 = V_1;
		NullCheck(L_2);
		int32_t L_4 = L_3;
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_5 = (L_2)->GetAt(static_cast<il2cpp_array_size_t>(L_4));
		V_2 = L_5;
		// if (it == null)
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_6 = V_2;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_7;
		L_7 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_6, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (L_7)
		{
			goto IL_0048;
		}
	}
	{
		// it.transform.SetParent(null, true);
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_8 = V_2;
		NullCheck(L_8);
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_9;
		L_9 = Component_get_transform_m2919A1D81931E6932C7F06D4C2F0AB8DDA9A5371(L_8, NULL);
		NullCheck(L_9);
		Transform_SetParent_m9BDD7B7476714B2D7919B10BDC22CE75C0A0A195(L_9, (Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1*)NULL, (bool)1, NULL);
		// if (Application.isPlaying)
		bool L_10;
		L_10 = Application_get_isPlaying_m25B0ABDFEF54F5370CD3F263A813540843D00F34(NULL);
		if (!L_10)
		{
			goto IL_003d;
		}
	}
	{
		// Destroy(it.gameObject);
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_11 = V_2;
		NullCheck(L_11);
		GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_12;
		L_12 = Component_get_gameObject_m57AEFBB14DB39EC476F740BA000E170355DE691B(L_11, NULL);
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		Object_Destroy_mE97D0A766419A81296E8D4E5C23D01D3FE91ACBB(L_12, NULL);
		goto IL_0048;
	}

IL_003d:
	{
		// DestroyImmediate(it.gameObject);
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_13 = V_2;
		NullCheck(L_13);
		GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_14;
		L_14 = Component_get_gameObject_m57AEFBB14DB39EC476F740BA000E170355DE691B(L_13, NULL);
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		Object_DestroyImmediate_m6336EBC83591A5DB64EC70C92132824C6E258705(L_14, NULL);
	}

IL_0048:
	{
		// for (int i = items.Length - 1; i >= 0; i--)
		int32_t L_15 = V_1;
		V_1 = ((int32_t)il2cpp_codegen_subtract(L_15, 1));
	}

IL_004c:
	{
		// for (int i = items.Length - 1; i >= 0; i--)
		int32_t L_16 = V_1;
		if ((((int32_t)L_16) >= ((int32_t)0)))
		{
			goto IL_000f;
		}
	}
	{
		// }
		return;
	}
}
// System.Void CrowdMatch.PixelGroup::ClearWalls()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelGroup_ClearWalls_m6830B5257A74968BB45CCAB6E99DFE82492320C9 (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Component_GetComponentsInChildren_TisWallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0_m9AD00368CDD148B6F031F3195DAF4512127EBA59_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	WallItemU5BU5D_tD1B1EF0B4FAE8376B92DB7B23ACCE192653C9B58* V_0 = NULL;
	int32_t V_1 = 0;
	WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* V_2 = NULL;
	{
		// var walls = GetComponentsInChildren<WallItem>();
		WallItemU5BU5D_tD1B1EF0B4FAE8376B92DB7B23ACCE192653C9B58* L_0;
		L_0 = Component_GetComponentsInChildren_TisWallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0_m9AD00368CDD148B6F031F3195DAF4512127EBA59(__this, Component_GetComponentsInChildren_TisWallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0_m9AD00368CDD148B6F031F3195DAF4512127EBA59_RuntimeMethod_var);
		V_0 = L_0;
		// for (int i = walls.Length - 1; i >= 0; i--)
		WallItemU5BU5D_tD1B1EF0B4FAE8376B92DB7B23ACCE192653C9B58* L_1 = V_0;
		NullCheck(L_1);
		V_1 = ((int32_t)il2cpp_codegen_subtract(((int32_t)(((RuntimeArray*)L_1)->max_length)), 1));
		goto IL_004c;
	}

IL_000f:
	{
		// var w = walls[i];
		WallItemU5BU5D_tD1B1EF0B4FAE8376B92DB7B23ACCE192653C9B58* L_2 = V_0;
		int32_t L_3 = V_1;
		NullCheck(L_2);
		int32_t L_4 = L_3;
		WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* L_5 = (L_2)->GetAt(static_cast<il2cpp_array_size_t>(L_4));
		V_2 = L_5;
		// if (w == null)
		WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* L_6 = V_2;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_7;
		L_7 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_6, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (L_7)
		{
			goto IL_0048;
		}
	}
	{
		// w.transform.SetParent(null, true);
		WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* L_8 = V_2;
		NullCheck(L_8);
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_9;
		L_9 = Component_get_transform_m2919A1D81931E6932C7F06D4C2F0AB8DDA9A5371(L_8, NULL);
		NullCheck(L_9);
		Transform_SetParent_m9BDD7B7476714B2D7919B10BDC22CE75C0A0A195(L_9, (Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1*)NULL, (bool)1, NULL);
		// if (Application.isPlaying)
		bool L_10;
		L_10 = Application_get_isPlaying_m25B0ABDFEF54F5370CD3F263A813540843D00F34(NULL);
		if (!L_10)
		{
			goto IL_003d;
		}
	}
	{
		// Destroy(w.gameObject);
		WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* L_11 = V_2;
		NullCheck(L_11);
		GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_12;
		L_12 = Component_get_gameObject_m57AEFBB14DB39EC476F740BA000E170355DE691B(L_11, NULL);
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		Object_Destroy_mE97D0A766419A81296E8D4E5C23D01D3FE91ACBB(L_12, NULL);
		goto IL_0048;
	}

IL_003d:
	{
		// DestroyImmediate(w.gameObject);
		WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* L_13 = V_2;
		NullCheck(L_13);
		GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_14;
		L_14 = Component_get_gameObject_m57AEFBB14DB39EC476F740BA000E170355DE691B(L_13, NULL);
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		Object_DestroyImmediate_m6336EBC83591A5DB64EC70C92132824C6E258705(L_14, NULL);
	}

IL_0048:
	{
		// for (int i = walls.Length - 1; i >= 0; i--)
		int32_t L_15 = V_1;
		V_1 = ((int32_t)il2cpp_codegen_subtract(L_15, 1));
	}

IL_004c:
	{
		// for (int i = walls.Length - 1; i >= 0; i--)
		int32_t L_16 = V_1;
		if ((((int32_t)L_16) >= ((int32_t)0)))
		{
			goto IL_000f;
		}
	}
	{
		// wallGrid = new bool[columns, TotalRows];
		int32_t L_17 = __this->___columns_7;
		int32_t L_18;
		L_18 = PixelGroup_get_TotalRows_m463568B781F3699B24713B03D51CB2A5084E2D79(__this, NULL);
		il2cpp_array_size_t L_20[] = { (il2cpp_array_size_t)L_17, (il2cpp_array_size_t)L_18 };
		BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6* L_19 = (BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6*)GenArrayNew(BooleanU5BU2CU5D_t0A96EF7DC71D7FB5C1757A719712D1DFB2D571B6_il2cpp_TypeInfo_var, L_20);
		__this->___wallGrid_17 = L_19;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___wallGrid_17), (void*)L_19);
		// }
		return;
	}
}
// CrowdMatch.WallItem CrowdMatch.PixelGroup::SpawnWall(System.Collections.Generic.IList`1<UnityEngine.Vector2>)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* PixelGroup_SpawnWall_m23602E58FD394CD60EA77B215E869D6FC95BA600 (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, RuntimeObject* ___points0, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Debug_t8394C7EEAECA3689C2C9B9DE9C7166D73596276F_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&GameObject_AddComponent_TisWallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0_mF275F42400D186788F6E0B363E9F0D081AB0FD8A_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&GameObject_t76FEDD663AB33C991A9C9A23129337651094216F_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IEnumerable_1_t8845214D7CADFAAD7AB98132A368905184A79DDF_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IEnumerator_1_t772680255A1A75379853D3823763B88BF026E055_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IEnumerator_t7B609C2FFA6EB5167D9C62A0C32A21DE2F666DAA_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1__ctor_m105596C2159C46B75E96D26ACEC0A5C1C1F5C5EC_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_Instantiate_TisGameObject_t76FEDD663AB33C991A9C9A23129337651094216F_m10D87C6E0708CA912BBB02555BF7D0FBC5D7A2B3_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral1E7C5AC3F45D7F269BDBDB108C7B98958BBA63CD);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral50639CAD49418C7B223CC529395C0E2A3892501C);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral90F54B7BFEE63FC7CF1A6ECC3EBE9DEFA4807B5E);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralC142EFACE770062722ED88219F1024199495E0EC);
		s_Il2CppMethodInitialized = true;
	}
	GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* V_0 = NULL;
	WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* V_1 = NULL;
	int32_t V_2 = 0;
	RuntimeObject* V_3 = NULL;
	Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A V_4;
	memset((&V_4), 0, sizeof(V_4));
	{
		// if (wallPrefab == null)
		GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_0 = __this->___wallPrefab_15;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_1;
		L_1 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_0, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_1)
		{
			goto IL_001a;
		}
	}
	{
		// Debug.LogError("[PixelGroup] wallPrefab ??????????????? Block ?????????????????");
		il2cpp_codegen_runtime_class_init_inline(Debug_t8394C7EEAECA3689C2C9B9DE9C7166D73596276F_il2cpp_TypeInfo_var);
		Debug_LogError_mB00B2B4468EF3CAF041B038D840820FB84C924B2(_stringLiteralC142EFACE770062722ED88219F1024199495E0EC, NULL);
		// return null;
		return (WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0*)NULL;
	}

IL_001a:
	{
		// var go = new GameObject("Wall_" + (transform.childCount + 1));
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_2;
		L_2 = Component_get_transform_m2919A1D81931E6932C7F06D4C2F0AB8DDA9A5371(__this, NULL);
		NullCheck(L_2);
		int32_t L_3;
		L_3 = Transform_get_childCount_mE9C29C702AB662CC540CA053EDE48BDAFA35B4B0(L_2, NULL);
		V_2 = ((int32_t)il2cpp_codegen_add(L_3, 1));
		String_t* L_4;
		L_4 = Int32_ToString_m030E01C24E294D6762FB0B6F37CB541581F55CA5((&V_2), NULL);
		String_t* L_5;
		L_5 = String_Concat_m9E3155FB84015C823606188F53B47CB44C444991(_stringLiteral90F54B7BFEE63FC7CF1A6ECC3EBE9DEFA4807B5E, L_4, NULL);
		GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_6 = (GameObject_t76FEDD663AB33C991A9C9A23129337651094216F*)il2cpp_codegen_object_new(GameObject_t76FEDD663AB33C991A9C9A23129337651094216F_il2cpp_TypeInfo_var);
		NullCheck(L_6);
		GameObject__ctor_m37D512B05D292F954792225E6C6EEE95293A9B88(L_6, L_5, NULL);
		V_0 = L_6;
		// go.transform.SetParent(transform, false);
		GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_7 = V_0;
		NullCheck(L_7);
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_8;
		L_8 = GameObject_get_transform_m0BC10ADFA1632166AE5544BDF9038A2650C2AE56(L_7, NULL);
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_9;
		L_9 = Component_get_transform_m2919A1D81931E6932C7F06D4C2F0AB8DDA9A5371(__this, NULL);
		NullCheck(L_8);
		Transform_SetParent_m9BDD7B7476714B2D7919B10BDC22CE75C0A0A195(L_8, L_9, (bool)0, NULL);
		// go.transform.localPosition = Vector3.zero;
		GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_10 = V_0;
		NullCheck(L_10);
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_11;
		L_11 = GameObject_get_transform_m0BC10ADFA1632166AE5544BDF9038A2650C2AE56(L_10, NULL);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_12;
		L_12 = Vector3_get_zero_m0C1249C3F25B1C70EAD3CC8B31259975A457AE39_inline(NULL);
		NullCheck(L_11);
		Transform_set_localPosition_mDE1C997F7D79C0885210B7732B4BA50EE7D73134(L_11, L_12, NULL);
		// var wall = go.AddComponent<WallItem>();
		GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_13 = V_0;
		NullCheck(L_13);
		WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* L_14;
		L_14 = GameObject_AddComponent_TisWallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0_mF275F42400D186788F6E0B363E9F0D081AB0FD8A(L_13, GameObject_AddComponent_TisWallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0_mF275F42400D186788F6E0B363E9F0D081AB0FD8A_RuntimeMethod_var);
		V_1 = L_14;
		// wall.points = new List<Vector2>(points);
		WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* L_15 = V_1;
		RuntimeObject* L_16 = ___points0;
		List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B* L_17 = (List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B*)il2cpp_codegen_object_new(List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B_il2cpp_TypeInfo_var);
		NullCheck(L_17);
		List_1__ctor_m105596C2159C46B75E96D26ACEC0A5C1C1F5C5EC(L_17, L_16, List_1__ctor_m105596C2159C46B75E96D26ACEC0A5C1C1F5C5EC_RuntimeMethod_var);
		NullCheck(L_15);
		L_15->___points_4 = L_17;
		Il2CppCodeGenWriteBarrier((void**)(&L_15->___points_4), (void*)L_17);
		// foreach (var cell in wall.EnumerateOccupiedCells())
		WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* L_18 = V_1;
		NullCheck(L_18);
		RuntimeObject* L_19;
		L_19 = WallItem_EnumerateOccupiedCells_mA15C2F1FBAAE6D7E2F190B8396AF069D35D717BF(L_18, NULL);
		NullCheck(L_19);
		RuntimeObject* L_20;
		L_20 = InterfaceFuncInvoker0< RuntimeObject* >::Invoke(0 /* System.Collections.Generic.IEnumerator`1<T> System.Collections.Generic.IEnumerable`1<UnityEngine.Vector2Int>::GetEnumerator() */, IEnumerable_1_t8845214D7CADFAAD7AB98132A368905184A79DDF_il2cpp_TypeInfo_var, L_19);
		V_3 = L_20;
	}
	{
		auto __finallyBlock = il2cpp::utils::Finally([&]
		{

FINALLY_013c:
			{// begin finally (depth: 1)
				{
					RuntimeObject* L_21 = V_3;
					if (!L_21)
					{
						goto IL_0145;
					}
				}
				{
					RuntimeObject* L_22 = V_3;
					NullCheck(L_22);
					InterfaceActionInvoker0::Invoke(0 /* System.Void System.IDisposable::Dispose() */, IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5_il2cpp_TypeInfo_var, L_22);
				}

IL_0145:
				{
					return;
				}
			}// end finally (depth: 1)
		});
		try
		{// begin try (depth: 1)
			{
				goto IL_012f_1;
			}

IL_0085_1:
			{
				// foreach (var cell in wall.EnumerateOccupiedCells())
				RuntimeObject* L_23 = V_3;
				NullCheck(L_23);
				Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A L_24;
				L_24 = InterfaceFuncInvoker0< Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A >::Invoke(0 /* T System.Collections.Generic.IEnumerator`1<UnityEngine.Vector2Int>::get_Current() */, IEnumerator_1_t772680255A1A75379853D3823763B88BF026E055_il2cpp_TypeInfo_var, L_23);
				V_4 = L_24;
				// if (!IsInRange(cell.x, cell.y))
				int32_t L_25;
				L_25 = Vector2Int_get_x_mA2CACB1B6E6B5AD0CCC32B2CD2EDCE3ECEB50576_inline((&V_4), NULL);
				int32_t L_26;
				L_26 = Vector2Int_get_y_m48454163ECF0B463FB5A16A0C4FC4B14DB0768B3_inline((&V_4), NULL);
				bool L_27;
				L_27 = PixelGroup_IsInRange_m7D51A009EE60A9ADDC0707368ADDDE95FCF788C3(__this, L_25, L_26, NULL);
				if (!L_27)
				{
					goto IL_012f_1;
				}
			}
			{
				// var block = Instantiate(wallPrefab);
				GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_28 = __this->___wallPrefab_15;
				il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
				GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_29;
				L_29 = Object_Instantiate_TisGameObject_t76FEDD663AB33C991A9C9A23129337651094216F_m10D87C6E0708CA912BBB02555BF7D0FBC5D7A2B3(L_28, Object_Instantiate_TisGameObject_t76FEDD663AB33C991A9C9A23129337651094216F_m10D87C6E0708CA912BBB02555BF7D0FBC5D7A2B3_RuntimeMethod_var);
				// block.name = "WallBlock_" + cell.y + "_" + cell.x;
				GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_30 = L_29;
				int32_t L_31;
				L_31 = Vector2Int_get_y_m48454163ECF0B463FB5A16A0C4FC4B14DB0768B3_inline((&V_4), NULL);
				V_2 = L_31;
				String_t* L_32;
				L_32 = Int32_ToString_m030E01C24E294D6762FB0B6F37CB541581F55CA5((&V_2), NULL);
				int32_t L_33;
				L_33 = Vector2Int_get_x_mA2CACB1B6E6B5AD0CCC32B2CD2EDCE3ECEB50576_inline((&V_4), NULL);
				V_2 = L_33;
				String_t* L_34;
				L_34 = Int32_ToString_m030E01C24E294D6762FB0B6F37CB541581F55CA5((&V_2), NULL);
				String_t* L_35;
				L_35 = String_Concat_m093934F71A9B351911EE46311674ED463B180006(_stringLiteral1E7C5AC3F45D7F269BDBDB108C7B98958BBA63CD, L_32, _stringLiteral50639CAD49418C7B223CC529395C0E2A3892501C, L_34, NULL);
				NullCheck(L_30);
				Object_set_name_mC79E6DC8FFD72479C90F0C4CC7F42A0FEAF5AE47(L_30, L_35, NULL);
				// block.transform.SetParent(go.transform, false);
				GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_36 = L_30;
				NullCheck(L_36);
				Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_37;
				L_37 = GameObject_get_transform_m0BC10ADFA1632166AE5544BDF9038A2650C2AE56(L_36, NULL);
				GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_38 = V_0;
				NullCheck(L_38);
				Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_39;
				L_39 = GameObject_get_transform_m0BC10ADFA1632166AE5544BDF9038A2650C2AE56(L_38, NULL);
				NullCheck(L_37);
				Transform_SetParent_m9BDD7B7476714B2D7919B10BDC22CE75C0A0A195(L_37, L_39, (bool)0, NULL);
				// block.transform.localPosition = GetLocalPosition(cell.x, cell.y);
				GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_40 = L_36;
				NullCheck(L_40);
				Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_41;
				L_41 = GameObject_get_transform_m0BC10ADFA1632166AE5544BDF9038A2650C2AE56(L_40, NULL);
				int32_t L_42;
				L_42 = Vector2Int_get_x_mA2CACB1B6E6B5AD0CCC32B2CD2EDCE3ECEB50576_inline((&V_4), NULL);
				int32_t L_43;
				L_43 = Vector2Int_get_y_m48454163ECF0B463FB5A16A0C4FC4B14DB0768B3_inline((&V_4), NULL);
				Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_44;
				L_44 = PixelGroup_GetLocalPosition_mA20E329BB838AB5B672C2A969C0E5B52A1771DD3(__this, L_42, L_43, NULL);
				NullCheck(L_41);
				Transform_set_localPosition_mDE1C997F7D79C0885210B7732B4BA50EE7D73134(L_41, L_44, NULL);
				// block.transform.localScale = Vector3.one * unitSize;
				NullCheck(L_40);
				Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_45;
				L_45 = GameObject_get_transform_m0BC10ADFA1632166AE5544BDF9038A2650C2AE56(L_40, NULL);
				Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_46;
				L_46 = Vector3_get_one_mC9B289F1E15C42C597180C9FE6FB492495B51D02_inline(NULL);
				float L_47 = __this->___unitSize_4;
				Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_48;
				L_48 = Vector3_op_Multiply_m87BA7C578F96C8E49BB07088DAAC4649F83B0353_inline(L_46, L_47, NULL);
				NullCheck(L_45);
				Transform_set_localScale_mBA79E811BAF6C47B80FF76414C12B47B3CD03633(L_45, L_48, NULL);
			}

IL_012f_1:
			{
				// foreach (var cell in wall.EnumerateOccupiedCells())
				RuntimeObject* L_49 = V_3;
				NullCheck(L_49);
				bool L_50;
				L_50 = InterfaceFuncInvoker0< bool >::Invoke(0 /* System.Boolean System.Collections.IEnumerator::MoveNext() */, IEnumerator_t7B609C2FFA6EB5167D9C62A0C32A21DE2F666DAA_il2cpp_TypeInfo_var, L_49);
				if (L_50)
				{
					goto IL_0085_1;
				}
			}
			{
				goto IL_0146;
			}
		}// end try (depth: 1)
		catch(Il2CppExceptionWrapper& e)
		{
			__finallyBlock.StoreException(e.ex);
		}
	}

IL_0146:
	{
		// return wall;
		WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* L_51 = V_1;
		return L_51;
	}
}
// CrowdMatch.PixelItem CrowdMatch.PixelGroup::SpawnPixel(System.Int32,System.Int32,System.Int32,CrowdMatch.ColorConfig)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* PixelGroup_SpawnPixel_m58C8AA0313CE16A654E53E3074445A62C447855C (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, int32_t ___col0, int32_t ___row1, int32_t ___colorId2, ColorConfig_tCFE08A98D99EE62711B453124E88292D1545172C* ___config3, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Debug_t8394C7EEAECA3689C2C9B9DE9C7166D73596276F_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&GameObject_GetComponent_TisPixelItem_t863890C77945A8C08435BF2F8859A47A413C794E_m7C12F9FAB885712E9777CD52832D09C1C0C4989B_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_Instantiate_TisGameObject_t76FEDD663AB33C991A9C9A23129337651094216F_m10D87C6E0708CA912BBB02555BF7D0FBC5D7A2B3_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral50639CAD49418C7B223CC529395C0E2A3892501C);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral6B8C303E7710B6904B487592D91528D80F4548B2);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral6F9A0883243199EB61F492C47190A43C05BCED11);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralF8511B43E7BE40B97DDDA39366F04722017C7098);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralFCC0421EA35E2D4A6C63BB7E474C10CA9BC2EB7B);
		s_Il2CppMethodInitialized = true;
	}
	PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* V_0 = NULL;
	{
		// if (pixelPrefab == null)
		GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_0 = __this->___pixelPrefab_14;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_1;
		L_1 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_0, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_1)
		{
			goto IL_001a;
		}
	}
	{
		// Debug.LogError("[PixelGroup] pixelPrefab ???????????? Block ??????? PixelItem ????");
		il2cpp_codegen_runtime_class_init_inline(Debug_t8394C7EEAECA3689C2C9B9DE9C7166D73596276F_il2cpp_TypeInfo_var);
		Debug_LogError_mB00B2B4468EF3CAF041B038D840820FB84C924B2(_stringLiteral6B8C303E7710B6904B487592D91528D80F4548B2, NULL);
		// return null;
		return (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E*)NULL;
	}

IL_001a:
	{
		// GameObject go = Instantiate(pixelPrefab);
		GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_2 = __this->___pixelPrefab_14;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_3;
		L_3 = Object_Instantiate_TisGameObject_t76FEDD663AB33C991A9C9A23129337651094216F_m10D87C6E0708CA912BBB02555BF7D0FBC5D7A2B3(L_2, Object_Instantiate_TisGameObject_t76FEDD663AB33C991A9C9A23129337651094216F_m10D87C6E0708CA912BBB02555BF7D0FBC5D7A2B3_RuntimeMethod_var);
		// go.name = "Pixel_" + row + "_" + col;
		GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_4 = L_3;
		String_t* L_5;
		L_5 = Int32_ToString_m030E01C24E294D6762FB0B6F37CB541581F55CA5((&___row1), NULL);
		String_t* L_6;
		L_6 = Int32_ToString_m030E01C24E294D6762FB0B6F37CB541581F55CA5((&___col0), NULL);
		String_t* L_7;
		L_7 = String_Concat_m093934F71A9B351911EE46311674ED463B180006(_stringLiteralFCC0421EA35E2D4A6C63BB7E474C10CA9BC2EB7B, L_5, _stringLiteral50639CAD49418C7B223CC529395C0E2A3892501C, L_6, NULL);
		NullCheck(L_4);
		Object_set_name_mC79E6DC8FFD72479C90F0C4CC7F42A0FEAF5AE47(L_4, L_7, NULL);
		// go.transform.SetParent(transform, false);
		GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_8 = L_4;
		NullCheck(L_8);
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_9;
		L_9 = GameObject_get_transform_m0BC10ADFA1632166AE5544BDF9038A2650C2AE56(L_8, NULL);
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_10;
		L_10 = Component_get_transform_m2919A1D81931E6932C7F06D4C2F0AB8DDA9A5371(__this, NULL);
		NullCheck(L_9);
		Transform_SetParent_m9BDD7B7476714B2D7919B10BDC22CE75C0A0A195(L_9, L_10, (bool)0, NULL);
		// go.transform.localPosition = GetLocalPosition(col, row);
		GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_11 = L_8;
		NullCheck(L_11);
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_12;
		L_12 = GameObject_get_transform_m0BC10ADFA1632166AE5544BDF9038A2650C2AE56(L_11, NULL);
		int32_t L_13 = ___col0;
		int32_t L_14 = ___row1;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_15;
		L_15 = PixelGroup_GetLocalPosition_mA20E329BB838AB5B672C2A969C0E5B52A1771DD3(__this, L_13, L_14, NULL);
		NullCheck(L_12);
		Transform_set_localPosition_mDE1C997F7D79C0885210B7732B4BA50EE7D73134(L_12, L_15, NULL);
		// go.transform.localScale = Vector3.one * unitSize;
		GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_16 = L_11;
		NullCheck(L_16);
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_17;
		L_17 = GameObject_get_transform_m0BC10ADFA1632166AE5544BDF9038A2650C2AE56(L_16, NULL);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_18;
		L_18 = Vector3_get_one_mC9B289F1E15C42C597180C9FE6FB492495B51D02_inline(NULL);
		float L_19 = __this->___unitSize_4;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_20;
		L_20 = Vector3_op_Multiply_m87BA7C578F96C8E49BB07088DAAC4649F83B0353_inline(L_18, L_19, NULL);
		NullCheck(L_17);
		Transform_set_localScale_mBA79E811BAF6C47B80FF76414C12B47B3CD03633(L_17, L_20, NULL);
		// var item = go.GetComponent<PixelItem>();
		NullCheck(L_16);
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_21;
		L_21 = GameObject_GetComponent_TisPixelItem_t863890C77945A8C08435BF2F8859A47A413C794E_m7C12F9FAB885712E9777CD52832D09C1C0C4989B(L_16, GameObject_GetComponent_TisPixelItem_t863890C77945A8C08435BF2F8859A47A413C794E_m7C12F9FAB885712E9777CD52832D09C1C0C4989B_RuntimeMethod_var);
		V_0 = L_21;
		// if (item == null)
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_22 = V_0;
		bool L_23;
		L_23 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_22, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_23)
		{
			goto IL_00b8;
		}
	}
	{
		// Debug.LogError("[PixelGroup] ??? " + pixelPrefab.name + " ?? PixelItem ???");
		GameObject_t76FEDD663AB33C991A9C9A23129337651094216F* L_24 = __this->___pixelPrefab_14;
		NullCheck(L_24);
		String_t* L_25;
		L_25 = Object_get_name_mAC2F6B897CF1303BA4249B4CB55271AFACBB6392(L_24, NULL);
		String_t* L_26;
		L_26 = String_Concat_m8855A6DE10F84DA7F4EC113CADDB59873A25573B(_stringLiteralF8511B43E7BE40B97DDDA39366F04722017C7098, L_25, _stringLiteral6F9A0883243199EB61F492C47190A43C05BCED11, NULL);
		il2cpp_codegen_runtime_class_init_inline(Debug_t8394C7EEAECA3689C2C9B9DE9C7166D73596276F_il2cpp_TypeInfo_var);
		Debug_LogError_mB00B2B4468EF3CAF041B038D840820FB84C924B2(L_26, NULL);
		// return null;
		return (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E*)NULL;
	}

IL_00b8:
	{
		// item.gridX = col;
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_27 = V_0;
		int32_t L_28 = ___col0;
		NullCheck(L_27);
		L_27->___gridX_5 = L_28;
		// item.gridZ = row;
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_29 = V_0;
		int32_t L_30 = ___row1;
		NullCheck(L_29);
		L_29->___gridZ_6 = L_30;
		// item.colorId = colorId;
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_31 = V_0;
		int32_t L_32 = ___colorId2;
		NullCheck(L_31);
		L_31->___colorId_4 = L_32;
		// item.ApplyMaterial(config);
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_33 = V_0;
		ColorConfig_tCFE08A98D99EE62711B453124E88292D1545172C* L_34 = ___config3;
		NullCheck(L_33);
		PixelItem_ApplyMaterial_m0CE545B161D5D3352A6C908088C9AC241381364F(L_33, L_34, NULL);
		// return item;
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_35 = V_0;
		return L_35;
	}
}
// System.Void CrowdMatch.PixelGroup::.ctor()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelGroup__ctor_mCB32AAE2C29323B59A673AA60B854437209E0E81 (PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&U3CPrivateImplementationDetailsU3E_t0F5473E849A5A5185A9F4C5246F0C32816C49FCA____CD9A54ED1F18BF97DB08914E280EA7349E11CA2C4885A4D8052552CEBA84208D_0_FieldInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		// public float unitSize = 1f;
		__this->___unitSize_4 = (1.0f);
		// public float spacingX = 0.1f;
		__this->___spacingX_5 = (0.100000001f);
		// public float spacingZ = 0.1f;
		__this->___spacingZ_6 = (0.100000001f);
		// public int columns = 5;
		__this->___columns_7 = 5;
		// public int rows = 5;
		__this->___rows_8 = 5;
		// public int[] colorIds = new int[] { 0, 1, 2, 3, 4, 5 };
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_0 = (Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C*)(Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C*)SZArrayNew(Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C_il2cpp_TypeInfo_var, (uint32_t)6);
		Int32U5BU5D_t19C97395396A72ECAF310612F0760F165060314C* L_1 = L_0;
		RuntimeFieldHandle_t6E4C45B6D2EA12FC99185805A7E77527899B25C5 L_2 = { reinterpret_cast<intptr_t> (U3CPrivateImplementationDetailsU3E_t0F5473E849A5A5185A9F4C5246F0C32816C49FCA____CD9A54ED1F18BF97DB08914E280EA7349E11CA2C4885A4D8052552CEBA84208D_0_FieldInfo_var) };
		RuntimeHelpers_InitializeArray_m751372AA3F24FBF6DA9B9D687CBFA2DE436CAB9B((RuntimeArray*)L_1, L_2, NULL);
		__this->___colorIds_10 = L_1;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___colorIds_10), (void*)L_1);
		// public int minRunLength = 2;
		__this->___minRunLength_11 = 2;
		// public int maxRunLength = 5;
		__this->___maxRunLength_12 = 5;
		// public bool fillToMultipleOf3 = true;
		__this->___fillToMultipleOf3_13 = (bool)1;
		MonoBehaviour__ctor_m592DB0105CA0BC97AA1C5F4AD27B12D68A3B7C1E(__this, NULL);
		return;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
// System.Boolean CrowdMatch.PixelItem::get_IsExposed()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool PixelItem_get_IsExposed_mC91D7897C85B411BACD2B7CA57A5D9473985125B (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, const RuntimeMethod* method) 
{
	{
		// public bool IsExposed { get; private set; }
		bool L_0 = __this->___U3CIsExposedU3Ek__BackingField_12;
		return L_0;
	}
}
// System.Void CrowdMatch.PixelItem::set_IsExposed(System.Boolean)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelItem_set_IsExposed_m7C2512B8A1E4DEB1D563791BDFB493BF5C98DBCB (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, bool ___value0, const RuntimeMethod* method) 
{
	{
		// public bool IsExposed { get; private set; }
		bool L_0 = ___value0;
		__this->___U3CIsExposedU3Ek__BackingField_12 = L_0;
		return;
	}
}
// UnityEngine.Transform CrowdMatch.PixelItem::get_Transform()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* PixelItem_get_Transform_mA941F56576D0184986617ED13F96869F91F09961 (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, const RuntimeMethod* method) 
{
	{
		// public Transform Transform => transform;
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_0;
		L_0 = Component_get_transform_m2919A1D81931E6932C7F06D4C2F0AB8DDA9A5371(__this, NULL);
		return L_0;
	}
}
// System.Void CrowdMatch.PixelItem::Awake()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelItem_Awake_mEEB75F4515A655AF1039A87B9704E45BC260D768 (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		// ApplyMaterial();
		PixelItem_ApplyMaterial_m0CE545B161D5D3352A6C908088C9AC241381364F(__this, (ColorConfig_tCFE08A98D99EE62711B453124E88292D1545172C*)NULL, NULL);
		// BindClickListener();
		PixelItem_BindClickListener_mB988349ABA6193468BE141196FE282ACB5CCDFB6(__this, NULL);
		// if (exposeMoveTarget != null)
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_0 = __this->___exposeMoveTarget_9;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_1;
		L_1 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_0, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_1)
		{
			goto IL_0031;
		}
	}
	{
		// _restLocalY = exposeMoveTarget.localPosition.y;
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_2 = __this->___exposeMoveTarget_9;
		NullCheck(L_2);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_3;
		L_3 = Transform_get_localPosition_mA9C86B990DF0685EA1061A120218993FDCC60A95(L_2, NULL);
		float L_4 = L_3.___y_3;
		__this->____restLocalY_17 = L_4;
	}

IL_0031:
	{
		// }
		return;
	}
}
// System.Void CrowdMatch.PixelItem::BindClickListener()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelItem_BindClickListener_mB988349ABA6193468BE141196FE282ACB5CCDFB6 (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Component_GetComponentInChildren_TisPixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524_m373C622F45C151B1A0749FD577AFDC09F0488041_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		// if (listener == null)
		PixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524* L_0 = __this->___listener_11;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_1;
		L_1 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_0, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_1)
		{
			goto IL_001b;
		}
	}
	{
		// listener = GetComponentInChildren<PixelClickListener>(true);
		PixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524* L_2;
		L_2 = Component_GetComponentInChildren_TisPixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524_m373C622F45C151B1A0749FD577AFDC09F0488041(__this, (bool)1, Component_GetComponentInChildren_TisPixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524_m373C622F45C151B1A0749FD577AFDC09F0488041_RuntimeMethod_var);
		__this->___listener_11 = L_2;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___listener_11), (void*)L_2);
	}

IL_001b:
	{
		// if (listener != null)
		PixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524* L_3 = __this->___listener_11;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_4;
		L_4 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_3, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_4)
		{
			goto IL_0035;
		}
	}
	{
		// listener.pixel = this;
		PixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524* L_5 = __this->___listener_11;
		NullCheck(L_5);
		L_5->___pixel_4 = __this;
		Il2CppCodeGenWriteBarrier((void**)(&L_5->___pixel_4), (void*)__this);
	}

IL_0035:
	{
		// }
		return;
	}
}
// System.Void CrowdMatch.PixelItem::SetClickable(System.Boolean)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelItem_SetClickable_m03A508D2784488E06E2C0D3E96222BE764078D7A (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, bool ___clickable0, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		// if (listener != null)
		PixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524* L_0 = __this->___listener_11;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_1;
		L_1 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_0, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_1)
		{
			goto IL_001a;
		}
	}
	{
		// listener.SetClickable(clickable);
		PixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524* L_2 = __this->___listener_11;
		bool L_3 = ___clickable0;
		NullCheck(L_2);
		PixelClickListener_SetClickable_mC408165A086B268E7D4D348CC90640DD8DCF5EC0(L_2, L_3, NULL);
	}

IL_001a:
	{
		// }
		return;
	}
}
// System.Void CrowdMatch.PixelItem::SetWalking(System.Boolean)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelItem_SetWalking_mEA83F37D0924BFD82287B01679016B81CAB96C50 (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, bool ___walking0, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		// if (animator == null)
		Animator_t8A52E42AE54F76681838FE9E632683EF3952E883* L_0 = __this->___animator_8;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_1;
		L_1 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_0, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_1)
		{
			goto IL_000f;
		}
	}
	{
		// return;
		return;
	}

IL_000f:
	{
		// _wantWalking = walking;
		bool L_2 = ___walking0;
		__this->____wantWalking_15 = L_2;
		// if (walking)
		bool L_3 = ___walking0;
		if (!L_3)
		{
			goto IL_0029;
		}
	}
	{
		// if (_smoothing)
		bool L_4 = __this->____smoothing_16;
		if (!L_4)
		{
			goto IL_0022;
		}
	}
	{
		// return;
		return;
	}

IL_0022:
	{
		// ApplyWalking();
		PixelItem_ApplyWalking_m96050E3E8F34DBE985E12C82E388E6F8481A1FED(__this, NULL);
		return;
	}

IL_0029:
	{
		// ApplyIdle();
		PixelItem_ApplyIdle_m4164B244DBE984A03C3739C59AE5F86C72077BE9(__this, NULL);
		// }
		return;
	}
}
// System.Void CrowdMatch.PixelItem::ApplyWalking()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelItem_ApplyWalking_m96050E3E8F34DBE985E12C82E388E6F8481A1FED (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral7F4682F559107108074468C96021CDFA3B5C0C04);
		s_Il2CppMethodInitialized = true;
	}
	{
		// animator.enabled = true;
		Animator_t8A52E42AE54F76681838FE9E632683EF3952E883* L_0 = __this->___animator_8;
		NullCheck(L_0);
		Behaviour_set_enabled_mF1DCFE60EB09E0529FE9476CA804A3AA2D72B16A(L_0, (bool)1, NULL);
		// animator.SetBool(WalkParam, true);
		Animator_t8A52E42AE54F76681838FE9E632683EF3952E883* L_1 = __this->___animator_8;
		NullCheck(L_1);
		Animator_SetBool_m6F8D4FAF0770CD4EC1F54406249785DE7391E42B(L_1, _stringLiteral7F4682F559107108074468C96021CDFA3B5C0C04, (bool)1, NULL);
		// animator.transform.DOKill();
		Animator_t8A52E42AE54F76681838FE9E632683EF3952E883* L_2 = __this->___animator_8;
		NullCheck(L_2);
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_3;
		L_3 = Component_get_transform_m2919A1D81931E6932C7F06D4C2F0AB8DDA9A5371(L_2, NULL);
		int32_t L_4;
		L_4 = ShortcutExtensions_DOKill_m3F197E779AB6CA95FF3C4C2DD547B4B493E42D46(L_3, (bool)0, NULL);
		// animator.transform.localPosition = Vector3.zero;
		Animator_t8A52E42AE54F76681838FE9E632683EF3952E883* L_5 = __this->___animator_8;
		NullCheck(L_5);
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_6;
		L_6 = Component_get_transform_m2919A1D81931E6932C7F06D4C2F0AB8DDA9A5371(L_5, NULL);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_7;
		L_7 = Vector3_get_zero_m0C1249C3F25B1C70EAD3CC8B31259975A457AE39_inline(NULL);
		NullCheck(L_6);
		Transform_set_localPosition_mDE1C997F7D79C0885210B7732B4BA50EE7D73134(L_6, L_7, NULL);
		// animator.transform.localRotation = Quaternion.identity;
		Animator_t8A52E42AE54F76681838FE9E632683EF3952E883* L_8 = __this->___animator_8;
		NullCheck(L_8);
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_9;
		L_9 = Component_get_transform_m2919A1D81931E6932C7F06D4C2F0AB8DDA9A5371(L_8, NULL);
		Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 L_10;
		L_10 = Quaternion_get_identity_m7E701AE095ED10FD5EA0B50ABCFDE2EEFF2173A5_inline(NULL);
		NullCheck(L_9);
		Transform_set_localRotation_mAB4A011D134BA58AB780BECC0025CA65F16185FA(L_9, L_10, NULL);
		// animator.applyRootMotion = true;
		Animator_t8A52E42AE54F76681838FE9E632683EF3952E883* L_11 = __this->___animator_8;
		NullCheck(L_11);
		Animator_set_applyRootMotion_mA0953B6AEE43D4AF0837365E7BFF60FCC74B0F98(L_11, (bool)1, NULL);
		// }
		return;
	}
}
// System.Void CrowdMatch.PixelItem::ApplyIdle()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelItem_ApplyIdle_m4164B244DBE984A03C3739C59AE5F86C72077BE9 (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&PixelItem_OnIdleSmoothComplete_mE4C6BF6BD71AD3B54E824F9C4C4844FECB9F8F99_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&TweenCallback_t7C8B8A38E7B30905FF1B83C943256EF23617BB24_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&TweenSettingsExtensions_OnComplete_TisTweenerCore_3_t392C54729BB024F5802F8E205C583653C4E886E3_m8BE213B05FF94E9AE889B180DB8B3DE6EC4EB1E1_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral7F4682F559107108074468C96021CDFA3B5C0C04);
		s_Il2CppMethodInitialized = true;
	}
	{
		// _smoothing = true;
		__this->____smoothing_16 = (bool)1;
		// animator.enabled = true;
		Animator_t8A52E42AE54F76681838FE9E632683EF3952E883* L_0 = __this->___animator_8;
		NullCheck(L_0);
		Behaviour_set_enabled_mF1DCFE60EB09E0529FE9476CA804A3AA2D72B16A(L_0, (bool)1, NULL);
		// animator.SetBool(WalkParam, false);
		Animator_t8A52E42AE54F76681838FE9E632683EF3952E883* L_1 = __this->___animator_8;
		NullCheck(L_1);
		Animator_SetBool_m6F8D4FAF0770CD4EC1F54406249785DE7391E42B(L_1, _stringLiteral7F4682F559107108074468C96021CDFA3B5C0C04, (bool)0, NULL);
		// animator.applyRootMotion = false;
		Animator_t8A52E42AE54F76681838FE9E632683EF3952E883* L_2 = __this->___animator_8;
		NullCheck(L_2);
		Animator_set_applyRootMotion_mA0953B6AEE43D4AF0837365E7BFF60FCC74B0F98(L_2, (bool)0, NULL);
		// animator.transform.DOKill();
		Animator_t8A52E42AE54F76681838FE9E632683EF3952E883* L_3 = __this->___animator_8;
		NullCheck(L_3);
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_4;
		L_4 = Component_get_transform_m2919A1D81931E6932C7F06D4C2F0AB8DDA9A5371(L_3, NULL);
		int32_t L_5;
		L_5 = ShortcutExtensions_DOKill_m3F197E779AB6CA95FF3C4C2DD547B4B493E42D46(L_4, (bool)0, NULL);
		// animator.transform.DOLocalMove(Vector3.zero, IdleResetDuration);
		Animator_t8A52E42AE54F76681838FE9E632683EF3952E883* L_6 = __this->___animator_8;
		NullCheck(L_6);
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_7;
		L_7 = Component_get_transform_m2919A1D81931E6932C7F06D4C2F0AB8DDA9A5371(L_6, NULL);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_8;
		L_8 = Vector3_get_zero_m0C1249C3F25B1C70EAD3CC8B31259975A457AE39_inline(NULL);
		TweenerCore_3_tCD82DFC45FB71C681FA8659EA63A7D7D16BFFE77* L_9;
		L_9 = ShortcutExtensions_DOLocalMove_m22F3EB581DADB5A3FC59B69F7F6F05A86F8E8348(L_7, L_8, (0.100000001f), (bool)0, NULL);
		// animator.transform.DOLocalRotate(Vector3.zero, IdleResetDuration)
		//     .OnComplete(OnIdleSmoothComplete);
		Animator_t8A52E42AE54F76681838FE9E632683EF3952E883* L_10 = __this->___animator_8;
		NullCheck(L_10);
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_11;
		L_11 = Component_get_transform_m2919A1D81931E6932C7F06D4C2F0AB8DDA9A5371(L_10, NULL);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_12;
		L_12 = Vector3_get_zero_m0C1249C3F25B1C70EAD3CC8B31259975A457AE39_inline(NULL);
		TweenerCore_3_t392C54729BB024F5802F8E205C583653C4E886E3* L_13;
		L_13 = ShortcutExtensions_DOLocalRotate_m6EB8F37963023C6B157C60013B98D2B612816DA4(L_11, L_12, (0.100000001f), 0, NULL);
		TweenCallback_t7C8B8A38E7B30905FF1B83C943256EF23617BB24* L_14 = (TweenCallback_t7C8B8A38E7B30905FF1B83C943256EF23617BB24*)il2cpp_codegen_object_new(TweenCallback_t7C8B8A38E7B30905FF1B83C943256EF23617BB24_il2cpp_TypeInfo_var);
		NullCheck(L_14);
		TweenCallback__ctor_m68CC9304423CBDE43001F9B1413B5DAAF70DB621(L_14, __this, (intptr_t)((void*)PixelItem_OnIdleSmoothComplete_mE4C6BF6BD71AD3B54E824F9C4C4844FECB9F8F99_RuntimeMethod_var), NULL);
		TweenerCore_3_t392C54729BB024F5802F8E205C583653C4E886E3* L_15;
		L_15 = TweenSettingsExtensions_OnComplete_TisTweenerCore_3_t392C54729BB024F5802F8E205C583653C4E886E3_m8BE213B05FF94E9AE889B180DB8B3DE6EC4EB1E1(L_13, L_14, TweenSettingsExtensions_OnComplete_TisTweenerCore_3_t392C54729BB024F5802F8E205C583653C4E886E3_m8BE213B05FF94E9AE889B180DB8B3DE6EC4EB1E1_RuntimeMethod_var);
		// }
		return;
	}
}
// System.Void CrowdMatch.PixelItem::OnIdleSmoothComplete()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelItem_OnIdleSmoothComplete_mE4C6BF6BD71AD3B54E824F9C4C4844FECB9F8F99 (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		// _smoothing = false;
		__this->____smoothing_16 = (bool)0;
		// if (_wantWalking && animator != null)
		bool L_0 = __this->____wantWalking_15;
		if (!L_0)
		{
			goto IL_0023;
		}
	}
	{
		Animator_t8A52E42AE54F76681838FE9E632683EF3952E883* L_1 = __this->___animator_8;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_2;
		L_2 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_1, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_2)
		{
			goto IL_0023;
		}
	}
	{
		// ApplyWalking();
		PixelItem_ApplyWalking_m96050E3E8F34DBE985E12C82E388E6F8481A1FED(__this, NULL);
	}

IL_0023:
	{
		// }
		return;
	}
}
// System.Void CrowdMatch.PixelItem::SitDownExposeTarget()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelItem_SitDownExposeTarget_m8269530699F2DD0526CFBF6ABA3F088BF296D817 (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		// if (exposeMoveTarget == null)
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_0 = __this->___exposeMoveTarget_9;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_1;
		L_1 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_0, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_1)
		{
			goto IL_000f;
		}
	}
	{
		// return;
		return;
	}

IL_000f:
	{
		// if (_exposeMove != null)
		Coroutine_t85EA685566A254C23F3FD77AB5BDFFFF8799596B* L_2 = __this->____exposeMove_18;
		if (!L_2)
		{
			goto IL_0023;
		}
	}
	{
		// StopCoroutine(_exposeMove);
		Coroutine_t85EA685566A254C23F3FD77AB5BDFFFF8799596B* L_3 = __this->____exposeMove_18;
		MonoBehaviour_StopCoroutine_mB0FC91BE84203BD8E360B3FBAE5B958B4C5ED22A(__this, L_3, NULL);
	}

IL_0023:
	{
		// _exposeMove = StartCoroutine(MoveExposeTargetToY(_restLocalY, exposeMoveDuration));
		float L_4 = __this->____restLocalY_17;
		float L_5 = __this->___exposeMoveDuration_10;
		RuntimeObject* L_6;
		L_6 = PixelItem_MoveExposeTargetToY_m6C859784BAA8F63BB768D507FB0F56E78C7A5AF1(__this, L_4, L_5, NULL);
		Coroutine_t85EA685566A254C23F3FD77AB5BDFFFF8799596B* L_7;
		L_7 = MonoBehaviour_StartCoroutine_m4CAFF732AA28CD3BDC5363B44A863575530EC812(__this, L_6, NULL);
		__this->____exposeMove_18 = L_7;
		Il2CppCodeGenWriteBarrier((void**)(&__this->____exposeMove_18), (void*)L_7);
		// }
		return;
	}
}
// System.Void CrowdMatch.PixelItem::SetColorId(System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelItem_SetColorId_m82A9BCEF81D369119B72D64816E71D8EC6C84FBB (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, int32_t ___id0, const RuntimeMethod* method) 
{
	{
		// colorId = id;
		int32_t L_0 = ___id0;
		__this->___colorId_4 = L_0;
		// ApplyMaterial();
		PixelItem_ApplyMaterial_m0CE545B161D5D3352A6C908088C9AC241381364F(__this, (ColorConfig_tCFE08A98D99EE62711B453124E88292D1545172C*)NULL, NULL);
		// }
		return;
	}
}
// System.Void CrowdMatch.PixelItem::ApplyMaterial(CrowdMatch.ColorConfig)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelItem_ApplyMaterial_m0CE545B161D5D3352A6C908088C9AC241381364F (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, ColorConfig_tCFE08A98D99EE62711B453124E88292D1545172C* ___config0, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Enumerator_Dispose_m39794B37E9AE88ED22C03824DE8D637C0DADBAF0_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Enumerator_MoveNext_m36545FD9B7C5DA66EAD80FD8813D279003BA8749_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Enumerator_get_Current_mF219FEB0F2097ED593A4E6E2283167335AFBA4B6_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_GetEnumerator_mC86A1EF9E784B7E7B5C00025383C6381B831F88C_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* V_0 = NULL;
	Enumerator_t4B5D90D1324DE6E043169A1E8DCD75512559AFA7 V_1;
	memset((&V_1), 0, sizeof(V_1));
	Renderer_t320575F223BCB177A982E5DDB5DB19FAA89E7FBF* V_2 = NULL;
	{
		// if (config == null)
		ColorConfig_tCFE08A98D99EE62711B453124E88292D1545172C* L_0 = ___config0;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_1;
		L_1 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_0, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_1)
		{
			goto IL_0022;
		}
	}
	{
		// if (GameManager.Instance != null)
		GameManager_t5B23045F478AD8B0E3D943ECC8493384B02FF32F* L_2;
		L_2 = GameManager_get_Instance_m5F7736EF916BFD34C734BE27B0EA4760C2D545FA_inline(NULL);
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_3;
		L_3 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_2, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_3)
		{
			goto IL_0022;
		}
	}
	{
		// config = GameManager.Instance.colorConfig;
		GameManager_t5B23045F478AD8B0E3D943ECC8493384B02FF32F* L_4;
		L_4 = GameManager_get_Instance_m5F7736EF916BFD34C734BE27B0EA4760C2D545FA_inline(NULL);
		NullCheck(L_4);
		ColorConfig_tCFE08A98D99EE62711B453124E88292D1545172C* L_5 = L_4->___colorConfig_5;
		___config0 = L_5;
	}

IL_0022:
	{
		// if (config == null)
		ColorConfig_tCFE08A98D99EE62711B453124E88292D1545172C* L_6 = ___config0;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_7;
		L_7 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_6, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_7)
		{
			goto IL_002c;
		}
	}
	{
		// return;
		return;
	}

IL_002c:
	{
		// var mat = config.GetMaterial(colorId);
		ColorConfig_tCFE08A98D99EE62711B453124E88292D1545172C* L_8 = ___config0;
		int32_t L_9 = __this->___colorId_4;
		NullCheck(L_8);
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_10;
		L_10 = ColorConfig_GetMaterial_mA4E421D41F9F046F6E209813332A86C790103589(L_8, L_9, NULL);
		V_0 = L_10;
		// if (mat == null)
		Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_11 = V_0;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_12;
		L_12 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_11, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_12)
		{
			goto IL_0043;
		}
	}
	{
		// return;
		return;
	}

IL_0043:
	{
		// foreach (var r in renderers)
		List_1_tD435DCC2A88E36DFC551EA5392CE0182F0C50E93* L_13 = __this->___renderers_7;
		NullCheck(L_13);
		Enumerator_t4B5D90D1324DE6E043169A1E8DCD75512559AFA7 L_14;
		L_14 = List_1_GetEnumerator_mC86A1EF9E784B7E7B5C00025383C6381B831F88C(L_13, List_1_GetEnumerator_mC86A1EF9E784B7E7B5C00025383C6381B831F88C_RuntimeMethod_var);
		V_1 = L_14;
	}
	{
		auto __finallyBlock = il2cpp::utils::Finally([&]
		{

FINALLY_0074:
			{// begin finally (depth: 1)
				Enumerator_Dispose_m39794B37E9AE88ED22C03824DE8D637C0DADBAF0((&V_1), Enumerator_Dispose_m39794B37E9AE88ED22C03824DE8D637C0DADBAF0_RuntimeMethod_var);
				return;
			}// end finally (depth: 1)
		});
		try
		{// begin try (depth: 1)
			{
				goto IL_0069_1;
			}

IL_0051_1:
			{
				// foreach (var r in renderers)
				Renderer_t320575F223BCB177A982E5DDB5DB19FAA89E7FBF* L_15;
				L_15 = Enumerator_get_Current_mF219FEB0F2097ED593A4E6E2283167335AFBA4B6_inline((&V_1), Enumerator_get_Current_mF219FEB0F2097ED593A4E6E2283167335AFBA4B6_RuntimeMethod_var);
				V_2 = L_15;
				// if (r != null)
				Renderer_t320575F223BCB177A982E5DDB5DB19FAA89E7FBF* L_16 = V_2;
				il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
				bool L_17;
				L_17 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_16, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
				if (!L_17)
				{
					goto IL_0069_1;
				}
			}
			{
				// r.sharedMaterial = mat;
				Renderer_t320575F223BCB177A982E5DDB5DB19FAA89E7FBF* L_18 = V_2;
				Material_t18053F08F347D0DCA5E1140EC7EC4533DD8A14E3* L_19 = V_0;
				NullCheck(L_18);
				Renderer_set_sharedMaterial_m5E842F9A06CFB7B77656EB319881CB4B3E8E4288(L_18, L_19, NULL);
			}

IL_0069_1:
			{
				// foreach (var r in renderers)
				bool L_20;
				L_20 = Enumerator_MoveNext_m36545FD9B7C5DA66EAD80FD8813D279003BA8749((&V_1), Enumerator_MoveNext_m36545FD9B7C5DA66EAD80FD8813D279003BA8749_RuntimeMethod_var);
				if (L_20)
				{
					goto IL_0051_1;
				}
			}
			{
				goto IL_0082;
			}
		}// end try (depth: 1)
		catch(Il2CppExceptionWrapper& e)
		{
			__finallyBlock.StoreException(e.ex);
		}
	}

IL_0082:
	{
		// }
		return;
	}
}
// System.Void CrowdMatch.PixelItem::SetExposed(System.Boolean)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelItem_SetExposed_mBDE7ACCCFA45DD1DE0B8271008CA72B124A07D95 (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, bool ___exposed0, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		// if (IsExposed == exposed)
		bool L_0;
		L_0 = PixelItem_get_IsExposed_mC91D7897C85B411BACD2B7CA57A5D9473985125B_inline(__this, NULL);
		bool L_1 = ___exposed0;
		if ((!(((uint32_t)L_0) == ((uint32_t)L_1))))
		{
			goto IL_000a;
		}
	}
	{
		// return;
		return;
	}

IL_000a:
	{
		// IsExposed = exposed;
		bool L_2 = ___exposed0;
		PixelItem_set_IsExposed_m7C2512B8A1E4DEB1D563791BDFB493BF5C98DBCB_inline(__this, L_2, NULL);
		// if (exposed)
		bool L_3 = ___exposed0;
		if (!L_3)
		{
			goto IL_0060;
		}
	}
	{
		// if (animator != null)
		Animator_t8A52E42AE54F76681838FE9E632683EF3952E883* L_4 = __this->___animator_8;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_5;
		L_5 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_4, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_5)
		{
			goto IL_002e;
		}
	}
	{
		// animator.enabled = true;
		Animator_t8A52E42AE54F76681838FE9E632683EF3952E883* L_6 = __this->___animator_8;
		NullCheck(L_6);
		Behaviour_set_enabled_mF1DCFE60EB09E0529FE9476CA804A3AA2D72B16A(L_6, (bool)1, NULL);
	}

IL_002e:
	{
		// if (_exposeMove != null)
		Coroutine_t85EA685566A254C23F3FD77AB5BDFFFF8799596B* L_7 = __this->____exposeMove_18;
		if (!L_7)
		{
			goto IL_0042;
		}
	}
	{
		// StopCoroutine(_exposeMove);
		Coroutine_t85EA685566A254C23F3FD77AB5BDFFFF8799596B* L_8 = __this->____exposeMove_18;
		MonoBehaviour_StopCoroutine_mB0FC91BE84203BD8E360B3FBAE5B958B4C5ED22A(__this, L_8, NULL);
	}

IL_0042:
	{
		// _exposeMove = StartCoroutine(MoveExposeTargetToY(0f, exposeMoveDuration));
		float L_9 = __this->___exposeMoveDuration_10;
		RuntimeObject* L_10;
		L_10 = PixelItem_MoveExposeTargetToY_m6C859784BAA8F63BB768D507FB0F56E78C7A5AF1(__this, (0.0f), L_9, NULL);
		Coroutine_t85EA685566A254C23F3FD77AB5BDFFFF8799596B* L_11;
		L_11 = MonoBehaviour_StartCoroutine_m4CAFF732AA28CD3BDC5363B44A863575530EC812(__this, L_10, NULL);
		__this->____exposeMove_18 = L_11;
		Il2CppCodeGenWriteBarrier((void**)(&__this->____exposeMove_18), (void*)L_11);
		return;
	}

IL_0060:
	{
		// if (_exposeMove != null)
		Coroutine_t85EA685566A254C23F3FD77AB5BDFFFF8799596B* L_12 = __this->____exposeMove_18;
		if (!L_12)
		{
			goto IL_007b;
		}
	}
	{
		// StopCoroutine(_exposeMove);
		Coroutine_t85EA685566A254C23F3FD77AB5BDFFFF8799596B* L_13 = __this->____exposeMove_18;
		MonoBehaviour_StopCoroutine_mB0FC91BE84203BD8E360B3FBAE5B958B4C5ED22A(__this, L_13, NULL);
		// _exposeMove = null;
		__this->____exposeMove_18 = (Coroutine_t85EA685566A254C23F3FD77AB5BDFFFF8799596B*)NULL;
		Il2CppCodeGenWriteBarrier((void**)(&__this->____exposeMove_18), (void*)(Coroutine_t85EA685566A254C23F3FD77AB5BDFFFF8799596B*)NULL);
	}

IL_007b:
	{
		// if (animator != null)
		Animator_t8A52E42AE54F76681838FE9E632683EF3952E883* L_14 = __this->___animator_8;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_15;
		L_15 = Object_op_Inequality_mD0BE578448EAA61948F25C32F8DD55AB1F778602(L_14, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_15)
		{
			goto IL_0095;
		}
	}
	{
		// animator.enabled = false;
		Animator_t8A52E42AE54F76681838FE9E632683EF3952E883* L_16 = __this->___animator_8;
		NullCheck(L_16);
		Behaviour_set_enabled_mF1DCFE60EB09E0529FE9476CA804A3AA2D72B16A(L_16, (bool)0, NULL);
	}

IL_0095:
	{
		// }
		return;
	}
}
// System.Collections.IEnumerator CrowdMatch.PixelItem::MoveExposeTargetToY(System.Single,System.Single)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* PixelItem_MoveExposeTargetToY_m6C859784BAA8F63BB768D507FB0F56E78C7A5AF1 (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, float ___targetY0, float ___duration1, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&U3CMoveExposeTargetToYU3Ed__35_tAD80531ED95A191AB90EE89508DFC17246399033_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		U3CMoveExposeTargetToYU3Ed__35_tAD80531ED95A191AB90EE89508DFC17246399033* L_0 = (U3CMoveExposeTargetToYU3Ed__35_tAD80531ED95A191AB90EE89508DFC17246399033*)il2cpp_codegen_object_new(U3CMoveExposeTargetToYU3Ed__35_tAD80531ED95A191AB90EE89508DFC17246399033_il2cpp_TypeInfo_var);
		NullCheck(L_0);
		U3CMoveExposeTargetToYU3Ed__35__ctor_mF04ADD1A971AB52742A40EAF409EAD8FA5CE22F2(L_0, 0, NULL);
		U3CMoveExposeTargetToYU3Ed__35_tAD80531ED95A191AB90EE89508DFC17246399033* L_1 = L_0;
		NullCheck(L_1);
		L_1->___U3CU3E4__this_2 = __this;
		Il2CppCodeGenWriteBarrier((void**)(&L_1->___U3CU3E4__this_2), (void*)__this);
		U3CMoveExposeTargetToYU3Ed__35_tAD80531ED95A191AB90EE89508DFC17246399033* L_2 = L_1;
		float L_3 = ___targetY0;
		NullCheck(L_2);
		L_2->___targetY_3 = L_3;
		U3CMoveExposeTargetToYU3Ed__35_tAD80531ED95A191AB90EE89508DFC17246399033* L_4 = L_2;
		float L_5 = ___duration1;
		NullCheck(L_4);
		L_4->___duration_4 = L_5;
		return L_4;
	}
}
// System.Void CrowdMatch.PixelItem::.ctor()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PixelItem__ctor_m8F7A766170C83C15A2F333E947847E990517E4FC (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1__ctor_m803E10F7A50EB22BF82C0C1AB251D5407B4496DE_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_tD435DCC2A88E36DFC551EA5392CE0182F0C50E93_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		// public List<Renderer> renderers = new List<Renderer>();
		List_1_tD435DCC2A88E36DFC551EA5392CE0182F0C50E93* L_0 = (List_1_tD435DCC2A88E36DFC551EA5392CE0182F0C50E93*)il2cpp_codegen_object_new(List_1_tD435DCC2A88E36DFC551EA5392CE0182F0C50E93_il2cpp_TypeInfo_var);
		NullCheck(L_0);
		List_1__ctor_m803E10F7A50EB22BF82C0C1AB251D5407B4496DE(L_0, List_1__ctor_m803E10F7A50EB22BF82C0C1AB251D5407B4496DE_RuntimeMethod_var);
		__this->___renderers_7 = L_0;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___renderers_7), (void*)L_0);
		// public float exposeMoveDuration = 0.3f;
		__this->___exposeMoveDuration_10 = (0.300000012f);
		// private float _restLocalY = -0.6957998f;
		__this->____restLocalY_17 = (-0.695799828f);
		MonoBehaviour__ctor_m592DB0105CA0BC97AA1C5F4AD27B12D68A3B7C1E(__this, NULL);
		return;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
// System.Void CrowdMatch.PixelItem/<MoveExposeTargetToY>d__35::.ctor(System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void U3CMoveExposeTargetToYU3Ed__35__ctor_mF04ADD1A971AB52742A40EAF409EAD8FA5CE22F2 (U3CMoveExposeTargetToYU3Ed__35_tAD80531ED95A191AB90EE89508DFC17246399033* __this, int32_t ___U3CU3E1__state0, const RuntimeMethod* method) 
{
	{
		Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2(__this, NULL);
		int32_t L_0 = ___U3CU3E1__state0;
		__this->___U3CU3E1__state_0 = L_0;
		return;
	}
}
// System.Void CrowdMatch.PixelItem/<MoveExposeTargetToY>d__35::System.IDisposable.Dispose()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void U3CMoveExposeTargetToYU3Ed__35_System_IDisposable_Dispose_m5338D94603EB8A8358A6F767C12450BDDC4B9C78 (U3CMoveExposeTargetToYU3Ed__35_tAD80531ED95A191AB90EE89508DFC17246399033* __this, const RuntimeMethod* method) 
{
	{
		return;
	}
}
// System.Boolean CrowdMatch.PixelItem/<MoveExposeTargetToY>d__35::MoveNext()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool U3CMoveExposeTargetToYU3Ed__35_MoveNext_m3069DD54C2E0D1F3B782A3873D90A3E74F61A985 (U3CMoveExposeTargetToYU3Ed__35_tAD80531ED95A191AB90EE89508DFC17246399033* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* V_1 = NULL;
	float V_2 = 0.0f;
	{
		int32_t L_0 = __this->___U3CU3E1__state_0;
		V_0 = L_0;
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_1 = __this->___U3CU3E4__this_2;
		V_1 = L_1;
		int32_t L_2 = V_0;
		if (!L_2)
		{
			goto IL_001a;
		}
	}
	{
		int32_t L_3 = V_0;
		if ((((int32_t)L_3) == ((int32_t)1)))
		{
			goto IL_010a;
		}
	}
	{
		return (bool)0;
	}

IL_001a:
	{
		__this->___U3CU3E1__state_0 = (-1);
		// if (exposeMoveTarget == null)
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_4 = V_1;
		NullCheck(L_4);
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_5 = L_4->___exposeMoveTarget_9;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_6;
		L_6 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_5, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_6)
		{
			goto IL_0031;
		}
	}
	{
		// yield break;
		return (bool)0;
	}

IL_0031:
	{
		// Transform t = exposeMoveTarget;
		PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* L_7 = V_1;
		NullCheck(L_7);
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_8 = L_7->___exposeMoveTarget_9;
		__this->___U3CtU3E5__2_5 = L_8;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___U3CtU3E5__2_5), (void*)L_8);
		// Vector3 start = t.localPosition;
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_9 = __this->___U3CtU3E5__2_5;
		NullCheck(L_9);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_10;
		L_10 = Transform_get_localPosition_mA9C86B990DF0685EA1061A120218993FDCC60A95(L_9, NULL);
		__this->___U3CstartU3E5__3_6 = L_10;
		// Vector3 target = new Vector3(start.x, targetY, start.z);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2* L_11 = (&__this->___U3CstartU3E5__3_6);
		float L_12 = L_11->___x_2;
		float L_13 = __this->___targetY_3;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2* L_14 = (&__this->___U3CstartU3E5__3_6);
		float L_15 = L_14->___z_4;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_16;
		memset((&L_16), 0, sizeof(L_16));
		Vector3__ctor_m376936E6B999EF1ECBE57D990A386303E2283DE0_inline((&L_16), L_12, L_13, L_15, /*hidden argument*/NULL);
		__this->___U3CtargetU3E5__4_7 = L_16;
		// float dur = Mathf.Max(0f, duration);
		float L_17 = __this->___duration_4;
		float L_18;
		L_18 = Mathf_Max_mF5379E63D2BBAC76D090748695D833934F8AD051_inline((0.0f), L_17, NULL);
		__this->___U3CdurU3E5__5_8 = L_18;
		// if (dur <= 0.0001f)
		float L_19 = __this->___U3CdurU3E5__5_8;
		if ((!(((float)L_19) <= ((float)(9.99999975E-05f)))))
		{
			goto IL_00ab;
		}
	}
	{
		// t.localPosition = target;
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_20 = __this->___U3CtU3E5__2_5;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_21 = __this->___U3CtargetU3E5__4_7;
		NullCheck(L_20);
		Transform_set_localPosition_mDE1C997F7D79C0885210B7732B4BA50EE7D73134(L_20, L_21, NULL);
		// yield break;
		return (bool)0;
	}

IL_00ab:
	{
		// float elapsed = 0f;
		__this->___U3CelapsedU3E5__6_9 = (0.0f);
		goto IL_0111;
	}

IL_00b8:
	{
		// elapsed += Time.deltaTime;
		float L_22 = __this->___U3CelapsedU3E5__6_9;
		float L_23;
		L_23 = Time_get_deltaTime_mC3195000401F0FD167DD2F948FD2BC58330D0865(NULL);
		__this->___U3CelapsedU3E5__6_9 = ((float)il2cpp_codegen_add(L_22, L_23));
		// float k = Mathf.Clamp01(elapsed / dur);
		float L_24 = __this->___U3CelapsedU3E5__6_9;
		float L_25 = __this->___U3CdurU3E5__5_8;
		float L_26;
		L_26 = Mathf_Clamp01_mA7E048DBDA832D399A581BE4D6DED9FA44CE0F14_inline(((float)(L_24/L_25)), NULL);
		V_2 = L_26;
		// t.localPosition = Vector3.Lerp(start, target, k);
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_27 = __this->___U3CtU3E5__2_5;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_28 = __this->___U3CstartU3E5__3_6;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_29 = __this->___U3CtargetU3E5__4_7;
		float L_30 = V_2;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_31;
		L_31 = Vector3_Lerp_m3A906D0530A94FAABB94F0F905E84D99BE85C3F8_inline(L_28, L_29, L_30, NULL);
		NullCheck(L_27);
		Transform_set_localPosition_mDE1C997F7D79C0885210B7732B4BA50EE7D73134(L_27, L_31, NULL);
		// yield return null;
		__this->___U3CU3E2__current_1 = NULL;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___U3CU3E2__current_1), (void*)NULL);
		__this->___U3CU3E1__state_0 = 1;
		return (bool)1;
	}

IL_010a:
	{
		__this->___U3CU3E1__state_0 = (-1);
	}

IL_0111:
	{
		// while (elapsed < dur)
		float L_32 = __this->___U3CelapsedU3E5__6_9;
		float L_33 = __this->___U3CdurU3E5__5_8;
		if ((((float)L_32) < ((float)L_33)))
		{
			goto IL_00b8;
		}
	}
	{
		// t.localPosition = target;
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_34 = __this->___U3CtU3E5__2_5;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_35 = __this->___U3CtargetU3E5__4_7;
		NullCheck(L_34);
		Transform_set_localPosition_mDE1C997F7D79C0885210B7732B4BA50EE7D73134(L_34, L_35, NULL);
		// }
		return (bool)0;
	}
}
// System.Object CrowdMatch.PixelItem/<MoveExposeTargetToY>d__35::System.Collections.Generic.IEnumerator<System.Object>.get_Current()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* U3CMoveExposeTargetToYU3Ed__35_System_Collections_Generic_IEnumeratorU3CSystem_ObjectU3E_get_Current_m4220E76493F7C47B9B1CAFF37B5D3798E9C6AB8A (U3CMoveExposeTargetToYU3Ed__35_tAD80531ED95A191AB90EE89508DFC17246399033* __this, const RuntimeMethod* method) 
{
	{
		RuntimeObject* L_0 = __this->___U3CU3E2__current_1;
		return L_0;
	}
}
// System.Void CrowdMatch.PixelItem/<MoveExposeTargetToY>d__35::System.Collections.IEnumerator.Reset()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void U3CMoveExposeTargetToYU3Ed__35_System_Collections_IEnumerator_Reset_mF4D2214C9CA9CABDC11398D89EE5351A6C7FDA02 (U3CMoveExposeTargetToYU3Ed__35_tAD80531ED95A191AB90EE89508DFC17246399033* __this, const RuntimeMethod* method) 
{
	{
		NotSupportedException_t1429765983D409BD2986508963C98D214E4EBF4A* L_0 = (NotSupportedException_t1429765983D409BD2986508963C98D214E4EBF4A*)il2cpp_codegen_object_new(((RuntimeClass*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&NotSupportedException_t1429765983D409BD2986508963C98D214E4EBF4A_il2cpp_TypeInfo_var)));
		NullCheck(L_0);
		NotSupportedException__ctor_m1398D0CDE19B36AA3DE9392879738C1EA2439CDF(L_0, NULL);
		IL2CPP_RAISE_MANAGED_EXCEPTION(L_0, ((RuntimeMethod*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&U3CMoveExposeTargetToYU3Ed__35_System_Collections_IEnumerator_Reset_mF4D2214C9CA9CABDC11398D89EE5351A6C7FDA02_RuntimeMethod_var)));
	}
}
// System.Object CrowdMatch.PixelItem/<MoveExposeTargetToY>d__35::System.Collections.IEnumerator.get_Current()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* U3CMoveExposeTargetToYU3Ed__35_System_Collections_IEnumerator_get_Current_m9827019F6136E3A3D784A5BACF4FCB780A325B13 (U3CMoveExposeTargetToYU3Ed__35_tAD80531ED95A191AB90EE89508DFC17246399033* __this, const RuntimeMethod* method) 
{
	{
		RuntimeObject* L_0 = __this->___U3CU3E2__current_1;
		return L_0;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
// System.Collections.Generic.List`1<System.ValueTuple`3<System.Int32,System.Int32,System.Int32>> CrowdMatch.SquareGridColorTool::Import(UnityEngine.Texture2D,System.Int32,UnityEngine.Color[],System.Func`3<System.Int32,System.Int32,System.Boolean>)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR List_1_t264DA9C475A0E3CF57148457209560CF2E2B6F27* SquareGridColorTool_Import_m5A3CEC91AE12BAF887E76E65CD19E4CC3F6C95B0 (Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* ___tex0, int32_t ___cellSize1, ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* ___palette2, Func_3_tE8F85DA3CAC4998201E5C56356280AFAB7185B69* ___isEnabled3, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_Add_m716377944B1E88CBB1B269AA9CF38C525A41D367_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1__ctor_m7734609AB0CE32B017F3FBC89E3A0D35323A5015_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_t264DA9C475A0E3CF57148457209560CF2E2B6F27_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&ValueTuple_3__ctor_m0D7E698F23721325245996D0B8DED1C102559F60_RuntimeMethod_var);
		s_Il2CppMethodInitialized = true;
	}
	List_1_t264DA9C475A0E3CF57148457209560CF2E2B6F27* V_0 = NULL;
	int32_t V_1 = 0;
	int32_t V_2 = 0;
	int32_t V_3 = 0;
	int32_t V_4 = 0;
	int32_t V_5 = 0;
	int32_t V_6 = 0;
	int32_t V_7 = 0;
	{
		// var result = new List<(int, int, int)>();
		List_1_t264DA9C475A0E3CF57148457209560CF2E2B6F27* L_0 = (List_1_t264DA9C475A0E3CF57148457209560CF2E2B6F27*)il2cpp_codegen_object_new(List_1_t264DA9C475A0E3CF57148457209560CF2E2B6F27_il2cpp_TypeInfo_var);
		NullCheck(L_0);
		List_1__ctor_m7734609AB0CE32B017F3FBC89E3A0D35323A5015(L_0, List_1__ctor_m7734609AB0CE32B017F3FBC89E3A0D35323A5015_RuntimeMethod_var);
		V_0 = L_0;
		// if (tex == null || cellSize <= 0) return result;
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_1 = ___tex0;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_2;
		L_2 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_1, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (L_2)
		{
			goto IL_0013;
		}
	}
	{
		int32_t L_3 = ___cellSize1;
		if ((((int32_t)L_3) > ((int32_t)0)))
		{
			goto IL_0015;
		}
	}

IL_0013:
	{
		// if (tex == null || cellSize <= 0) return result;
		List_1_t264DA9C475A0E3CF57148457209560CF2E2B6F27* L_4 = V_0;
		return L_4;
	}

IL_0015:
	{
		// int cols = tex.width / cellSize;
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_5 = ___tex0;
		NullCheck(L_5);
		int32_t L_6;
		L_6 = VirtualFuncInvoker0< int32_t >::Invoke(4 /* System.Int32 UnityEngine.Texture::get_width() */, L_5);
		int32_t L_7 = ___cellSize1;
		V_1 = ((int32_t)(L_6/L_7));
		// int rows = tex.height / cellSize;
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_8 = ___tex0;
		NullCheck(L_8);
		int32_t L_9;
		L_9 = VirtualFuncInvoker0< int32_t >::Invoke(6 /* System.Int32 UnityEngine.Texture::get_height() */, L_8);
		int32_t L_10 = ___cellSize1;
		V_2 = ((int32_t)(L_9/L_10));
		// if (cols == 0 || rows == 0) return result;
		int32_t L_11 = V_1;
		if (!L_11)
		{
			goto IL_002d;
		}
	}
	{
		int32_t L_12 = V_2;
		if (L_12)
		{
			goto IL_002f;
		}
	}

IL_002d:
	{
		// if (cols == 0 || rows == 0) return result;
		List_1_t264DA9C475A0E3CF57148457209560CF2E2B6F27* L_13 = V_0;
		return L_13;
	}

IL_002f:
	{
		// for (int row = 0; row < rows; row++)
		V_3 = 0;
		goto IL_00ae;
	}

IL_0033:
	{
		// for (int col = 0; col < cols; col++)
		V_4 = 0;
		goto IL_00a5;
	}

IL_0038:
	{
		// if (isEnabled != null && !isEnabled(col, row)) continue;
		Func_3_tE8F85DA3CAC4998201E5C56356280AFAB7185B69* L_14 = ___isEnabled3;
		if (!L_14)
		{
			goto IL_0046;
		}
	}
	{
		Func_3_tE8F85DA3CAC4998201E5C56356280AFAB7185B69* L_15 = ___isEnabled3;
		int32_t L_16 = V_4;
		int32_t L_17 = V_3;
		NullCheck(L_15);
		bool L_18;
		L_18 = Func_3_Invoke_m5C4CCADFF1AE4540F252182089A9BF3CBE7BAFE6_inline(L_15, L_16, L_17, NULL);
		if (!L_18)
		{
			goto IL_009f;
		}
	}

IL_0046:
	{
		// int px = col * cellSize + cellSize / 2;
		int32_t L_19 = V_4;
		int32_t L_20 = ___cellSize1;
		int32_t L_21 = ___cellSize1;
		V_5 = ((int32_t)il2cpp_codegen_add(((int32_t)il2cpp_codegen_multiply(L_19, L_20)), ((int32_t)(L_21/2))));
		// int py = row * cellSize + cellSize / 2;
		int32_t L_22 = V_3;
		int32_t L_23 = ___cellSize1;
		int32_t L_24 = ___cellSize1;
		V_6 = ((int32_t)il2cpp_codegen_add(((int32_t)il2cpp_codegen_multiply(L_22, L_23)), ((int32_t)(L_24/2))));
		// px = Mathf.Clamp(px, 0, tex.width - 1);
		int32_t L_25 = V_5;
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_26 = ___tex0;
		NullCheck(L_26);
		int32_t L_27;
		L_27 = VirtualFuncInvoker0< int32_t >::Invoke(4 /* System.Int32 UnityEngine.Texture::get_width() */, L_26);
		int32_t L_28;
		L_28 = Mathf_Clamp_m4DC36EEFDBE5F07C16249DA568023C5ECCFF0E7B_inline(L_25, 0, ((int32_t)il2cpp_codegen_subtract(L_27, 1)), NULL);
		V_5 = L_28;
		// py = Mathf.Clamp(py, 0, tex.height - 1);
		int32_t L_29 = V_6;
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_30 = ___tex0;
		NullCheck(L_30);
		int32_t L_31;
		L_31 = VirtualFuncInvoker0< int32_t >::Invoke(6 /* System.Int32 UnityEngine.Texture::get_height() */, L_30);
		int32_t L_32;
		L_32 = Mathf_Clamp_m4DC36EEFDBE5F07C16249DA568023C5ECCFF0E7B_inline(L_29, 0, ((int32_t)il2cpp_codegen_subtract(L_31, 1)), NULL);
		V_6 = L_32;
		// int colorIndex = GridColorMatcher.FindClosestColorIndex(tex.GetPixel(px, py), palette);
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_33 = ___tex0;
		int32_t L_34 = V_5;
		int32_t L_35 = V_6;
		NullCheck(L_33);
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_36;
		L_36 = Texture2D_GetPixel_m69A17FE5CC220F438C7421DCB50A9E22AAB4A415(L_33, L_34, L_35, NULL);
		ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* L_37 = ___palette2;
		int32_t L_38;
		L_38 = GridColorMatcher_FindClosestColorIndex_mC714B659FAA530B6286BC986B6B3FD98645B5729(L_36, L_37, NULL);
		V_7 = L_38;
		// result.Add((col, row, colorIndex));
		List_1_t264DA9C475A0E3CF57148457209560CF2E2B6F27* L_39 = V_0;
		int32_t L_40 = V_4;
		int32_t L_41 = V_3;
		int32_t L_42 = V_7;
		ValueTuple_3_tBFE24970AFB182B3C77F4C747E3F1E7A7B4EBC57 L_43;
		memset((&L_43), 0, sizeof(L_43));
		ValueTuple_3__ctor_m0D7E698F23721325245996D0B8DED1C102559F60((&L_43), L_40, L_41, L_42, /*hidden argument*/ValueTuple_3__ctor_m0D7E698F23721325245996D0B8DED1C102559F60_RuntimeMethod_var);
		NullCheck(L_39);
		List_1_Add_m716377944B1E88CBB1B269AA9CF38C525A41D367_inline(L_39, L_43, List_1_Add_m716377944B1E88CBB1B269AA9CF38C525A41D367_RuntimeMethod_var);
	}

IL_009f:
	{
		// for (int col = 0; col < cols; col++)
		int32_t L_44 = V_4;
		V_4 = ((int32_t)il2cpp_codegen_add(L_44, 1));
	}

IL_00a5:
	{
		// for (int col = 0; col < cols; col++)
		int32_t L_45 = V_4;
		int32_t L_46 = V_1;
		if ((((int32_t)L_45) < ((int32_t)L_46)))
		{
			goto IL_0038;
		}
	}
	{
		// for (int row = 0; row < rows; row++)
		int32_t L_47 = V_3;
		V_3 = ((int32_t)il2cpp_codegen_add(L_47, 1));
	}

IL_00ae:
	{
		// for (int row = 0; row < rows; row++)
		int32_t L_48 = V_3;
		int32_t L_49 = V_2;
		if ((((int32_t)L_48) < ((int32_t)L_49)))
		{
			goto IL_0033;
		}
	}
	{
		// return result;
		List_1_t264DA9C475A0E3CF57148457209560CF2E2B6F27* L_50 = V_0;
		return L_50;
	}
}
// UnityEngine.Texture2D CrowdMatch.SquareGridColorTool::Export(System.Int32,System.Int32,System.Int32,System.Func`3<System.Int32,System.Int32,System.Nullable`1<UnityEngine.Color>>,System.Nullable`1<UnityEngine.Color>)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* SquareGridColorTool_Export_m6975AC89EF41ABBB8E967A0134E45B386CCF357E (int32_t ___cols0, int32_t ___rows1, int32_t ___cellSize2, Func_3_t171A20B65D1078C91C9A875EF6CBB2716C7E322D* ___getColor3, Nullable_1_tEE83D90B507D40B6C58B5EEF5B9D44D377B44F11 ___clearColor4, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Nullable_1_GetValueOrDefault_m9A7869C021F041D45F2A851F70F97F8114AC99E4_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Nullable_1_get_HasValue_mFAF0B4EEA878E596C80258FE3BDA57CEF40C8D7F_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Nullable_1_get_Value_m3FC15B40E747AA6A9F2AB10A00C46ABE84393637_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	int32_t V_1 = 0;
	Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* V_2 = NULL;
	Color_tD001788D726C3A7F1379BEED0260B9591F440C1F V_3;
	memset((&V_3), 0, sizeof(V_3));
	ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* V_4 = NULL;
	Nullable_1_tEE83D90B507D40B6C58B5EEF5B9D44D377B44F11 V_5;
	memset((&V_5), 0, sizeof(V_5));
	int32_t V_6 = 0;
	int32_t V_7 = 0;
	int32_t V_8 = 0;
	Nullable_1_tEE83D90B507D40B6C58B5EEF5B9D44D377B44F11 V_9;
	memset((&V_9), 0, sizeof(V_9));
	int32_t V_10 = 0;
	int32_t V_11 = 0;
	int32_t V_12 = 0;
	int32_t V_13 = 0;
	Color_tD001788D726C3A7F1379BEED0260B9591F440C1F G_B3_0;
	memset((&G_B3_0), 0, sizeof(G_B3_0));
	Nullable_1_tEE83D90B507D40B6C58B5EEF5B9D44D377B44F11 G_B11_0;
	memset((&G_B11_0), 0, sizeof(G_B11_0));
	{
		// int w = Mathf.Max(1, cols * cellSize);
		int32_t L_0 = ___cols0;
		int32_t L_1 = ___cellSize2;
		int32_t L_2;
		L_2 = Mathf_Max_m7FA442918DE37E3A00106D1F2E789D65829792B8_inline(1, ((int32_t)il2cpp_codegen_multiply(L_0, L_1)), NULL);
		V_0 = L_2;
		// int h = Mathf.Max(1, rows * cellSize);
		int32_t L_3 = ___rows1;
		int32_t L_4 = ___cellSize2;
		int32_t L_5;
		L_5 = Mathf_Max_m7FA442918DE37E3A00106D1F2E789D65829792B8_inline(1, ((int32_t)il2cpp_codegen_multiply(L_3, L_4)), NULL);
		V_1 = L_5;
		// Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
		int32_t L_6 = V_0;
		int32_t L_7 = V_1;
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_8 = (Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4*)il2cpp_codegen_object_new(Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4_il2cpp_TypeInfo_var);
		NullCheck(L_8);
		Texture2D__ctor_mECF60A9EC0638EC353C02C8E99B6B465D23BE917(L_8, L_6, L_7, 4, (bool)0, NULL);
		V_2 = L_8;
		// Color background = clearColor ?? Color.white;
		Nullable_1_tEE83D90B507D40B6C58B5EEF5B9D44D377B44F11 L_9 = ___clearColor4;
		V_5 = L_9;
		bool L_10;
		L_10 = Nullable_1_get_HasValue_mFAF0B4EEA878E596C80258FE3BDA57CEF40C8D7F_inline((&V_5), Nullable_1_get_HasValue_mFAF0B4EEA878E596C80258FE3BDA57CEF40C8D7F_RuntimeMethod_var);
		if (L_10)
		{
			goto IL_0032;
		}
	}
	{
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_11;
		L_11 = Color_get_white_m068F5AF879B0FCA584E3693F762EA41BB65532C6_inline(NULL);
		G_B3_0 = L_11;
		goto IL_0039;
	}

IL_0032:
	{
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_12;
		L_12 = Nullable_1_GetValueOrDefault_m9A7869C021F041D45F2A851F70F97F8114AC99E4_inline((&V_5), Nullable_1_GetValueOrDefault_m9A7869C021F041D45F2A851F70F97F8114AC99E4_RuntimeMethod_var);
		G_B3_0 = L_12;
	}

IL_0039:
	{
		V_3 = G_B3_0;
		// Color[] pixels = new Color[w * h];
		int32_t L_13 = V_0;
		int32_t L_14 = V_1;
		ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* L_15 = (ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389*)(ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389*)SZArrayNew(ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389_il2cpp_TypeInfo_var, (uint32_t)((int32_t)il2cpp_codegen_multiply(L_13, L_14)));
		V_4 = L_15;
		// for (int i = 0; i < pixels.Length; i++)
		V_6 = 0;
		goto IL_0059;
	}

IL_0049:
	{
		// pixels[i] = background;
		ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* L_16 = V_4;
		int32_t L_17 = V_6;
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_18 = V_3;
		NullCheck(L_16);
		(L_16)->SetAt(static_cast<il2cpp_array_size_t>(L_17), (Color_tD001788D726C3A7F1379BEED0260B9591F440C1F)L_18);
		// for (int i = 0; i < pixels.Length; i++)
		int32_t L_19 = V_6;
		V_6 = ((int32_t)il2cpp_codegen_add(L_19, 1));
	}

IL_0059:
	{
		// for (int i = 0; i < pixels.Length; i++)
		int32_t L_20 = V_6;
		ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* L_21 = V_4;
		NullCheck(L_21);
		if ((((int32_t)L_20) < ((int32_t)((int32_t)(((RuntimeArray*)L_21)->max_length)))))
		{
			goto IL_0049;
		}
	}
	{
		// for (int row = 0; row < rows; row++)
		V_7 = 0;
		goto IL_00fa;
	}

IL_0069:
	{
		// for (int col = 0; col < cols; col++)
		V_8 = 0;
		goto IL_00ec;
	}

IL_006e:
	{
		// Color? c = getColor?.Invoke(col, row);
		Func_3_t171A20B65D1078C91C9A875EF6CBB2716C7E322D* L_22 = ___getColor3;
		if (L_22)
		{
			goto IL_007d;
		}
	}
	{
		il2cpp_codegen_initobj((&V_5), sizeof(Nullable_1_tEE83D90B507D40B6C58B5EEF5B9D44D377B44F11));
		Nullable_1_tEE83D90B507D40B6C58B5EEF5B9D44D377B44F11 L_23 = V_5;
		G_B11_0 = L_23;
		goto IL_0087;
	}

IL_007d:
	{
		Func_3_t171A20B65D1078C91C9A875EF6CBB2716C7E322D* L_24 = ___getColor3;
		int32_t L_25 = V_8;
		int32_t L_26 = V_7;
		NullCheck(L_24);
		Nullable_1_tEE83D90B507D40B6C58B5EEF5B9D44D377B44F11 L_27;
		L_27 = Func_3_Invoke_mADC4326AE0426011BAE02568945B03277B225B79_inline(L_24, L_25, L_26, NULL);
		G_B11_0 = L_27;
	}

IL_0087:
	{
		V_9 = G_B11_0;
		// if (!c.HasValue) continue;
		bool L_28;
		L_28 = Nullable_1_get_HasValue_mFAF0B4EEA878E596C80258FE3BDA57CEF40C8D7F_inline((&V_9), Nullable_1_get_HasValue_mFAF0B4EEA878E596C80258FE3BDA57CEF40C8D7F_RuntimeMethod_var);
		if (!L_28)
		{
			goto IL_00e6;
		}
	}
	{
		// for (int dy = 0; dy < cellSize; dy++)
		V_10 = 0;
		goto IL_00e1;
	}

IL_0097:
	{
		// for (int dx = 0; dx < cellSize; dx++)
		V_11 = 0;
		goto IL_00d6;
	}

IL_009c:
	{
		// int px = col * cellSize + dx;
		int32_t L_29 = V_8;
		int32_t L_30 = ___cellSize2;
		int32_t L_31 = V_11;
		V_12 = ((int32_t)il2cpp_codegen_add(((int32_t)il2cpp_codegen_multiply(L_29, L_30)), L_31));
		// int py = row * cellSize + dy;
		int32_t L_32 = V_7;
		int32_t L_33 = ___cellSize2;
		int32_t L_34 = V_10;
		// int idx = py * w + px;
		int32_t L_35 = V_0;
		int32_t L_36 = V_12;
		V_13 = ((int32_t)il2cpp_codegen_add(((int32_t)il2cpp_codegen_multiply(((int32_t)il2cpp_codegen_add(((int32_t)il2cpp_codegen_multiply(L_32, L_33)), L_34)), L_35)), L_36));
		// if (idx >= 0 && idx < pixels.Length)
		int32_t L_37 = V_13;
		if ((((int32_t)L_37) < ((int32_t)0)))
		{
			goto IL_00d0;
		}
	}
	{
		int32_t L_38 = V_13;
		ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* L_39 = V_4;
		NullCheck(L_39);
		if ((((int32_t)L_38) >= ((int32_t)((int32_t)(((RuntimeArray*)L_39)->max_length)))))
		{
			goto IL_00d0;
		}
	}
	{
		// pixels[idx] = c.Value;
		ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* L_40 = V_4;
		int32_t L_41 = V_13;
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_42;
		L_42 = Nullable_1_get_Value_m3FC15B40E747AA6A9F2AB10A00C46ABE84393637((&V_9), Nullable_1_get_Value_m3FC15B40E747AA6A9F2AB10A00C46ABE84393637_RuntimeMethod_var);
		NullCheck(L_40);
		(L_40)->SetAt(static_cast<il2cpp_array_size_t>(L_41), (Color_tD001788D726C3A7F1379BEED0260B9591F440C1F)L_42);
	}

IL_00d0:
	{
		// for (int dx = 0; dx < cellSize; dx++)
		int32_t L_43 = V_11;
		V_11 = ((int32_t)il2cpp_codegen_add(L_43, 1));
	}

IL_00d6:
	{
		// for (int dx = 0; dx < cellSize; dx++)
		int32_t L_44 = V_11;
		int32_t L_45 = ___cellSize2;
		if ((((int32_t)L_44) < ((int32_t)L_45)))
		{
			goto IL_009c;
		}
	}
	{
		// for (int dy = 0; dy < cellSize; dy++)
		int32_t L_46 = V_10;
		V_10 = ((int32_t)il2cpp_codegen_add(L_46, 1));
	}

IL_00e1:
	{
		// for (int dy = 0; dy < cellSize; dy++)
		int32_t L_47 = V_10;
		int32_t L_48 = ___cellSize2;
		if ((((int32_t)L_47) < ((int32_t)L_48)))
		{
			goto IL_0097;
		}
	}

IL_00e6:
	{
		// for (int col = 0; col < cols; col++)
		int32_t L_49 = V_8;
		V_8 = ((int32_t)il2cpp_codegen_add(L_49, 1));
	}

IL_00ec:
	{
		// for (int col = 0; col < cols; col++)
		int32_t L_50 = V_8;
		int32_t L_51 = ___cols0;
		if ((((int32_t)L_50) < ((int32_t)L_51)))
		{
			goto IL_006e;
		}
	}
	{
		// for (int row = 0; row < rows; row++)
		int32_t L_52 = V_7;
		V_7 = ((int32_t)il2cpp_codegen_add(L_52, 1));
	}

IL_00fa:
	{
		// for (int row = 0; row < rows; row++)
		int32_t L_53 = V_7;
		int32_t L_54 = ___rows1;
		if ((((int32_t)L_53) < ((int32_t)L_54)))
		{
			goto IL_0069;
		}
	}
	{
		// tex.SetPixels(pixels);
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_55 = V_2;
		ColorU5BU5D_t612261CF293F6FFC3D80AB52259FF0DC2B2CC389* L_56 = V_4;
		NullCheck(L_55);
		Texture2D_SetPixels_mAE0CDFA15FA96F840D7FFADC31405D8AF20D9073(L_55, L_56, NULL);
		// tex.Apply();
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_57 = V_2;
		NullCheck(L_57);
		Texture2D_Apply_mA014182C9EE0BBF6EEE3B286854F29E50EB972DC(L_57, NULL);
		// return tex;
		Texture2D_tE6505BC111DD8A424A9DBE8E05D7D09E11FFFCF4* L_58 = V_2;
		return L_58;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
// CrowdMatch.PixelGroup CrowdMatch.WallItem::get_Group()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* WallItem_get_Group_m467F5ED6CCE4DFEFEBF4F86532BE7ED2CD614729 (WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Component_GetComponentInParent_TisPixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80_mD60D9A2DE017B170590FDF6B2CB4CB66A737CD4F_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		// if (_gizmoGroup == null)
		PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* L_0 = __this->____gizmoGroup_8;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_1;
		L_1 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_0, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_1)
		{
			goto IL_001a;
		}
	}
	{
		// _gizmoGroup = GetComponentInParent<PixelGroup>();
		PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* L_2;
		L_2 = Component_GetComponentInParent_TisPixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80_mD60D9A2DE017B170590FDF6B2CB4CB66A737CD4F(__this, Component_GetComponentInParent_TisPixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80_mD60D9A2DE017B170590FDF6B2CB4CB66A737CD4F_RuntimeMethod_var);
		__this->____gizmoGroup_8 = L_2;
		Il2CppCodeGenWriteBarrier((void**)(&__this->____gizmoGroup_8), (void*)L_2);
	}

IL_001a:
	{
		// return _gizmoGroup;
		PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* L_3 = __this->____gizmoGroup_8;
		return L_3;
	}
}
// UnityEngine.Vector2Int CrowdMatch.WallItem::ToCell(UnityEngine.Vector2)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A WallItem_ToCell_mFA83E7FB1FBAB3694E1F9640EC1E3988748B61C6 (Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 ___p0, const RuntimeMethod* method) 
{
	{
		// return new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));
		Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 L_0 = ___p0;
		float L_1 = L_0.___x_0;
		int32_t L_2;
		L_2 = Mathf_RoundToInt_m60F8B66CF27F1FA75AA219342BD184B75771EB4B_inline(L_1, NULL);
		Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 L_3 = ___p0;
		float L_4 = L_3.___y_1;
		int32_t L_5;
		L_5 = Mathf_RoundToInt_m60F8B66CF27F1FA75AA219342BD184B75771EB4B_inline(L_4, NULL);
		Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A L_6;
		memset((&L_6), 0, sizeof(L_6));
		Vector2Int__ctor_mC20D1312133EB8CB63EC11067088B043660F11CE_inline((&L_6), L_2, L_5, /*hidden argument*/NULL);
		return L_6;
	}
}
// System.Boolean CrowdMatch.WallItem::IsValid(System.String&)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool WallItem_IsValid_mC1B9EFD7323ADD6C068F934892236F94CC89A39A (WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* __this, String_t** ___error0, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_get_Count_mF1C0C56457C655BDFFC6EE5B46FAD8BAEC1F588B_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_get_Item_m1F8E226CAD72B83C5E75BB66B43025247806B543_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral12326FAECC370AC4E02673E03510901F14A2F650);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteral73D9C88DD061503C8E495188F237E1901308C684);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralD36070345E1BBE825940C76A43B5AD5F33F3FC62);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralEDE2495E1435E5A58A340546D3AB772C2D72C193);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 V_1;
	memset((&V_1), 0, sizeof(V_1));
	Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 V_2;
	memset((&V_2), 0, sizeof(V_2));
	int32_t V_3 = 0;
	Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 V_4;
	memset((&V_4), 0, sizeof(V_4));
	{
		// for (int i = 0; i + 1 < points.Count; i++)
		V_0 = 0;
		goto IL_00b0;
	}

IL_0007:
	{
		// Vector2 a = points[i];
		List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B* L_0 = __this->___points_4;
		int32_t L_1 = V_0;
		NullCheck(L_0);
		Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 L_2;
		L_2 = List_1_get_Item_m1F8E226CAD72B83C5E75BB66B43025247806B543(L_0, L_1, List_1_get_Item_m1F8E226CAD72B83C5E75BB66B43025247806B543_RuntimeMethod_var);
		V_1 = L_2;
		// Vector2 b = points[i + 1];
		List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B* L_3 = __this->___points_4;
		int32_t L_4 = V_0;
		NullCheck(L_3);
		Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 L_5;
		L_5 = List_1_get_Item_m1F8E226CAD72B83C5E75BB66B43025247806B543(L_3, ((int32_t)il2cpp_codegen_add(L_4, 1)), List_1_get_Item_m1F8E226CAD72B83C5E75BB66B43025247806B543_RuntimeMethod_var);
		V_2 = L_5;
		// if (!Mathf.Approximately(a.x, b.x) && !Mathf.Approximately(a.y, b.y))
		Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 L_6 = V_1;
		float L_7 = L_6.___x_0;
		Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 L_8 = V_2;
		float L_9 = L_8.___x_0;
		bool L_10;
		L_10 = Mathf_Approximately_m1DADD012A8FC82E11FB282501AE2EBBF9A77150B_inline(L_7, L_9, NULL);
		if (L_10)
		{
			goto IL_00ac;
		}
	}
	{
		Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 L_11 = V_1;
		float L_12 = L_11.___y_1;
		Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 L_13 = V_2;
		float L_14 = L_13.___y_1;
		bool L_15;
		L_15 = Mathf_Approximately_m1DADD012A8FC82E11FB282501AE2EBBF9A77150B_inline(L_12, L_14, NULL);
		if (L_15)
		{
			goto IL_00ac;
		}
	}
	{
		// error = "? " + (i + 1) + " ??" + a + " ? " + b + "????? X/Z ??????? x ? y ???";
		String_t** L_16 = ___error0;
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_17 = (StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248*)(StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248*)SZArrayNew(StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248_il2cpp_TypeInfo_var, (uint32_t)7);
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_18 = L_17;
		NullCheck(L_18);
		ArrayElementTypeCheck (L_18, _stringLiteralD36070345E1BBE825940C76A43B5AD5F33F3FC62);
		(L_18)->SetAt(static_cast<il2cpp_array_size_t>(0), (String_t*)_stringLiteralD36070345E1BBE825940C76A43B5AD5F33F3FC62);
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_19 = L_18;
		int32_t L_20 = V_0;
		V_3 = ((int32_t)il2cpp_codegen_add(L_20, 1));
		String_t* L_21;
		L_21 = Int32_ToString_m030E01C24E294D6762FB0B6F37CB541581F55CA5((&V_3), NULL);
		NullCheck(L_19);
		ArrayElementTypeCheck (L_19, L_21);
		(L_19)->SetAt(static_cast<il2cpp_array_size_t>(1), (String_t*)L_21);
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_22 = L_19;
		NullCheck(L_22);
		ArrayElementTypeCheck (L_22, _stringLiteralEDE2495E1435E5A58A340546D3AB772C2D72C193);
		(L_22)->SetAt(static_cast<il2cpp_array_size_t>(2), (String_t*)_stringLiteralEDE2495E1435E5A58A340546D3AB772C2D72C193);
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_23 = L_22;
		Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 L_24 = V_1;
		V_4 = L_24;
		String_t* L_25;
		L_25 = Vector2_ToString_mB47B29ECB21FA3A4ACEABEFA18077A5A6BBCCB27((&V_4), NULL);
		NullCheck(L_23);
		ArrayElementTypeCheck (L_23, L_25);
		(L_23)->SetAt(static_cast<il2cpp_array_size_t>(3), (String_t*)L_25);
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_26 = L_23;
		NullCheck(L_26);
		ArrayElementTypeCheck (L_26, _stringLiteral73D9C88DD061503C8E495188F237E1901308C684);
		(L_26)->SetAt(static_cast<il2cpp_array_size_t>(4), (String_t*)_stringLiteral73D9C88DD061503C8E495188F237E1901308C684);
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_27 = L_26;
		Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 L_28 = V_2;
		V_4 = L_28;
		String_t* L_29;
		L_29 = Vector2_ToString_mB47B29ECB21FA3A4ACEABEFA18077A5A6BBCCB27((&V_4), NULL);
		NullCheck(L_27);
		ArrayElementTypeCheck (L_27, L_29);
		(L_27)->SetAt(static_cast<il2cpp_array_size_t>(5), (String_t*)L_29);
		StringU5BU5D_t7674CD946EC0CE7B3AE0BE70E6EE85F2ECD9F248* L_30 = L_27;
		NullCheck(L_30);
		ArrayElementTypeCheck (L_30, _stringLiteral12326FAECC370AC4E02673E03510901F14A2F650);
		(L_30)->SetAt(static_cast<il2cpp_array_size_t>(6), (String_t*)_stringLiteral12326FAECC370AC4E02673E03510901F14A2F650);
		String_t* L_31;
		L_31 = String_Concat_m647EBF831F54B6DF7D5AFA5FD012CF4EE7571B6A(L_30, NULL);
		*((RuntimeObject**)L_16) = (RuntimeObject*)L_31;
		Il2CppCodeGenWriteBarrier((void**)(RuntimeObject**)L_16, (void*)(RuntimeObject*)L_31);
		// return false;
		return (bool)0;
	}

IL_00ac:
	{
		// for (int i = 0; i + 1 < points.Count; i++)
		int32_t L_32 = V_0;
		V_0 = ((int32_t)il2cpp_codegen_add(L_32, 1));
	}

IL_00b0:
	{
		// for (int i = 0; i + 1 < points.Count; i++)
		int32_t L_33 = V_0;
		List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B* L_34 = __this->___points_4;
		NullCheck(L_34);
		int32_t L_35;
		L_35 = List_1_get_Count_mF1C0C56457C655BDFFC6EE5B46FAD8BAEC1F588B_inline(L_34, List_1_get_Count_mF1C0C56457C655BDFFC6EE5B46FAD8BAEC1F588B_RuntimeMethod_var);
		if ((((int32_t)((int32_t)il2cpp_codegen_add(L_33, 1))) < ((int32_t)L_35)))
		{
			goto IL_0007;
		}
	}
	{
		// error = null;
		String_t** L_36 = ___error0;
		*((RuntimeObject**)L_36) = (RuntimeObject*)NULL;
		Il2CppCodeGenWriteBarrier((void**)(RuntimeObject**)L_36, (void*)(RuntimeObject*)NULL);
		// return true;
		return (bool)1;
	}
}
// System.Void CrowdMatch.WallItem::EnumerateSegment(UnityEngine.Vector2,UnityEngine.Vector2,System.Collections.Generic.HashSet`1<UnityEngine.Vector2Int>)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void WallItem_EnumerateSegment_mFBA3CBBEBD03941811D1207EE8559B01C5C2D5BC (Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 ___a0, Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 ___b1, HashSet_1_t55B946E0B889BDE32B3004D0822A6C5017AFA406* ___set2, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&HashSet_1_Add_m517B9238E386530A854B4286891358CC8327A7B6_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A V_0;
	memset((&V_0), 0, sizeof(V_0));
	Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A V_1;
	memset((&V_1), 0, sizeof(V_1));
	int32_t V_2 = 0;
	int32_t V_3 = 0;
	int32_t V_4 = 0;
	int32_t V_5 = 0;
	{
		// Vector2Int ca = ToCell(a);
		Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 L_0 = ___a0;
		Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A L_1;
		L_1 = WallItem_ToCell_mFA83E7FB1FBAB3694E1F9640EC1E3988748B61C6(L_0, NULL);
		V_0 = L_1;
		// Vector2Int cb = ToCell(b);
		Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 L_2 = ___b1;
		Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A L_3;
		L_3 = WallItem_ToCell_mFA83E7FB1FBAB3694E1F9640EC1E3988748B61C6(L_2, NULL);
		V_1 = L_3;
		// int steps = Mathf.Max(Mathf.Abs(cb.x - ca.x), Mathf.Abs(cb.y - ca.y));
		int32_t L_4;
		L_4 = Vector2Int_get_x_mA2CACB1B6E6B5AD0CCC32B2CD2EDCE3ECEB50576_inline((&V_1), NULL);
		int32_t L_5;
		L_5 = Vector2Int_get_x_mA2CACB1B6E6B5AD0CCC32B2CD2EDCE3ECEB50576_inline((&V_0), NULL);
		int32_t L_6;
		L_6 = Mathf_Abs_mD945EDDEA0D62D21BFDBAB7B1C0F18DFF1CEC905_inline(((int32_t)il2cpp_codegen_subtract(L_4, L_5)), NULL);
		int32_t L_7;
		L_7 = Vector2Int_get_y_m48454163ECF0B463FB5A16A0C4FC4B14DB0768B3_inline((&V_1), NULL);
		int32_t L_8;
		L_8 = Vector2Int_get_y_m48454163ECF0B463FB5A16A0C4FC4B14DB0768B3_inline((&V_0), NULL);
		int32_t L_9;
		L_9 = Mathf_Abs_mD945EDDEA0D62D21BFDBAB7B1C0F18DFF1CEC905_inline(((int32_t)il2cpp_codegen_subtract(L_7, L_8)), NULL);
		int32_t L_10;
		L_10 = Mathf_Max_m7FA442918DE37E3A00106D1F2E789D65829792B8_inline(L_6, L_9, NULL);
		V_2 = L_10;
		// int dx = System.Math.Sign(cb.x - ca.x);
		int32_t L_11;
		L_11 = Vector2Int_get_x_mA2CACB1B6E6B5AD0CCC32B2CD2EDCE3ECEB50576_inline((&V_1), NULL);
		int32_t L_12;
		L_12 = Vector2Int_get_x_mA2CACB1B6E6B5AD0CCC32B2CD2EDCE3ECEB50576_inline((&V_0), NULL);
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		int32_t L_13;
		L_13 = Math_Sign_m1E922CDC910F3DAFCC8DB60D4C33BA00CF1AB5D4(((int32_t)il2cpp_codegen_subtract(L_11, L_12)), NULL);
		V_3 = L_13;
		// int dy = System.Math.Sign(cb.y - ca.y);
		int32_t L_14;
		L_14 = Vector2Int_get_y_m48454163ECF0B463FB5A16A0C4FC4B14DB0768B3_inline((&V_1), NULL);
		int32_t L_15;
		L_15 = Vector2Int_get_y_m48454163ECF0B463FB5A16A0C4FC4B14DB0768B3_inline((&V_0), NULL);
		int32_t L_16;
		L_16 = Math_Sign_m1E922CDC910F3DAFCC8DB60D4C33BA00CF1AB5D4(((int32_t)il2cpp_codegen_subtract(L_14, L_15)), NULL);
		V_4 = L_16;
		// set.Add(ca);
		HashSet_1_t55B946E0B889BDE32B3004D0822A6C5017AFA406* L_17 = ___set2;
		Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A L_18 = V_0;
		NullCheck(L_17);
		bool L_19;
		L_19 = HashSet_1_Add_m517B9238E386530A854B4286891358CC8327A7B6(L_17, L_18, HashSet_1_Add_m517B9238E386530A854B4286891358CC8327A7B6_RuntimeMethod_var);
		// for (int i = 1; i <= steps; i++)
		V_5 = 1;
		goto IL_009f;
	}

IL_0074:
	{
		// set.Add(new Vector2Int(ca.x + dx * i, ca.y + dy * i));
		HashSet_1_t55B946E0B889BDE32B3004D0822A6C5017AFA406* L_20 = ___set2;
		int32_t L_21;
		L_21 = Vector2Int_get_x_mA2CACB1B6E6B5AD0CCC32B2CD2EDCE3ECEB50576_inline((&V_0), NULL);
		int32_t L_22 = V_3;
		int32_t L_23 = V_5;
		int32_t L_24;
		L_24 = Vector2Int_get_y_m48454163ECF0B463FB5A16A0C4FC4B14DB0768B3_inline((&V_0), NULL);
		int32_t L_25 = V_4;
		int32_t L_26 = V_5;
		Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A L_27;
		memset((&L_27), 0, sizeof(L_27));
		Vector2Int__ctor_mC20D1312133EB8CB63EC11067088B043660F11CE_inline((&L_27), ((int32_t)il2cpp_codegen_add(L_21, ((int32_t)il2cpp_codegen_multiply(L_22, L_23)))), ((int32_t)il2cpp_codegen_add(L_24, ((int32_t)il2cpp_codegen_multiply(L_25, L_26)))), /*hidden argument*/NULL);
		NullCheck(L_20);
		bool L_28;
		L_28 = HashSet_1_Add_m517B9238E386530A854B4286891358CC8327A7B6(L_20, L_27, HashSet_1_Add_m517B9238E386530A854B4286891358CC8327A7B6_RuntimeMethod_var);
		// for (int i = 1; i <= steps; i++)
		int32_t L_29 = V_5;
		V_5 = ((int32_t)il2cpp_codegen_add(L_29, 1));
	}

IL_009f:
	{
		// for (int i = 1; i <= steps; i++)
		int32_t L_30 = V_5;
		int32_t L_31 = V_2;
		if ((((int32_t)L_30) <= ((int32_t)L_31)))
		{
			goto IL_0074;
		}
	}
	{
		// }
		return;
	}
}
// System.Void CrowdMatch.WallItem::CollectOccupiedCells(System.Collections.Generic.IReadOnlyList`1<UnityEngine.Vector2>,System.Collections.Generic.HashSet`1<UnityEngine.Vector2Int>)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void WallItem_CollectOccupiedCells_m726DF1E889297C025677C298A7FD3128CE48BF70 (RuntimeObject* ___points0, HashSet_1_t55B946E0B889BDE32B3004D0822A6C5017AFA406* ___set1, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IReadOnlyCollection_1_t846EB7065157C69B6F0123614957234D9A0AC8D9_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IReadOnlyList_1_t3067BC0A09F7D5ADBABEE74BCB8640FACCBC19A0_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	{
		// if (points == null)
		RuntimeObject* L_0 = ___points0;
		if (L_0)
		{
			goto IL_0004;
		}
	}
	{
		// return;
		return;
	}

IL_0004:
	{
		// for (int i = 0; i + 1 < points.Count; i++)
		V_0 = 0;
		goto IL_0022;
	}

IL_0008:
	{
		// EnumerateSegment(points[i], points[i + 1], set);
		RuntimeObject* L_1 = ___points0;
		int32_t L_2 = V_0;
		NullCheck(L_1);
		Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 L_3;
		L_3 = InterfaceFuncInvoker1< Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7, int32_t >::Invoke(0 /* T System.Collections.Generic.IReadOnlyList`1<UnityEngine.Vector2>::get_Item(System.Int32) */, IReadOnlyList_1_t3067BC0A09F7D5ADBABEE74BCB8640FACCBC19A0_il2cpp_TypeInfo_var, L_1, L_2);
		RuntimeObject* L_4 = ___points0;
		int32_t L_5 = V_0;
		NullCheck(L_4);
		Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 L_6;
		L_6 = InterfaceFuncInvoker1< Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7, int32_t >::Invoke(0 /* T System.Collections.Generic.IReadOnlyList`1<UnityEngine.Vector2>::get_Item(System.Int32) */, IReadOnlyList_1_t3067BC0A09F7D5ADBABEE74BCB8640FACCBC19A0_il2cpp_TypeInfo_var, L_4, ((int32_t)il2cpp_codegen_add(L_5, 1)));
		HashSet_1_t55B946E0B889BDE32B3004D0822A6C5017AFA406* L_7 = ___set1;
		WallItem_EnumerateSegment_mFBA3CBBEBD03941811D1207EE8559B01C5C2D5BC(L_3, L_6, L_7, NULL);
		// for (int i = 0; i + 1 < points.Count; i++)
		int32_t L_8 = V_0;
		V_0 = ((int32_t)il2cpp_codegen_add(L_8, 1));
	}

IL_0022:
	{
		// for (int i = 0; i + 1 < points.Count; i++)
		int32_t L_9 = V_0;
		RuntimeObject* L_10 = ___points0;
		NullCheck(L_10);
		int32_t L_11;
		L_11 = InterfaceFuncInvoker0< int32_t >::Invoke(0 /* System.Int32 System.Collections.Generic.IReadOnlyCollection`1<UnityEngine.Vector2>::get_Count() */, IReadOnlyCollection_1_t846EB7065157C69B6F0123614957234D9A0AC8D9_il2cpp_TypeInfo_var, L_10);
		if ((((int32_t)((int32_t)il2cpp_codegen_add(L_9, 1))) < ((int32_t)L_11)))
		{
			goto IL_0008;
		}
	}
	{
		// }
		return;
	}
}
// System.Collections.Generic.IEnumerable`1<UnityEngine.Vector2Int> CrowdMatch.WallItem::EnumerateOccupiedCells()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR RuntimeObject* WallItem_EnumerateOccupiedCells_mA15C2F1FBAAE6D7E2F190B8396AF069D35D717BF (WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&HashSet_1__ctor_m0E27874668BB3B3160062D69799276CFEF8072AB_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&HashSet_1_t55B946E0B889BDE32B3004D0822A6C5017AFA406_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	HashSet_1_t55B946E0B889BDE32B3004D0822A6C5017AFA406* V_0 = NULL;
	{
		// var set = new HashSet<Vector2Int>();
		HashSet_1_t55B946E0B889BDE32B3004D0822A6C5017AFA406* L_0 = (HashSet_1_t55B946E0B889BDE32B3004D0822A6C5017AFA406*)il2cpp_codegen_object_new(HashSet_1_t55B946E0B889BDE32B3004D0822A6C5017AFA406_il2cpp_TypeInfo_var);
		NullCheck(L_0);
		HashSet_1__ctor_m0E27874668BB3B3160062D69799276CFEF8072AB(L_0, HashSet_1__ctor_m0E27874668BB3B3160062D69799276CFEF8072AB_RuntimeMethod_var);
		V_0 = L_0;
		// CollectOccupiedCells(points, set);
		List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B* L_1 = __this->___points_4;
		HashSet_1_t55B946E0B889BDE32B3004D0822A6C5017AFA406* L_2 = V_0;
		WallItem_CollectOccupiedCells_m726DF1E889297C025677C298A7FD3128CE48BF70(L_1, L_2, NULL);
		// return set;
		HashSet_1_t55B946E0B889BDE32B3004D0822A6C5017AFA406* L_3 = V_0;
		return L_3;
	}
}
// System.Int32 CrowdMatch.WallItem::OccupiedCellCount()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t WallItem_OccupiedCellCount_m9F665B80EB9190D5F550E0A9096A0EA3CA1C3AFC (WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IEnumerable_1_t8845214D7CADFAAD7AB98132A368905184A79DDF_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IEnumerator_1_t772680255A1A75379853D3823763B88BF026E055_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IEnumerator_t7B609C2FFA6EB5167D9C62A0C32A21DE2F666DAA_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	RuntimeObject* V_1 = NULL;
	{
		// int n = 0;
		V_0 = 0;
		// foreach (var _ in EnumerateOccupiedCells())
		RuntimeObject* L_0;
		L_0 = WallItem_EnumerateOccupiedCells_mA15C2F1FBAAE6D7E2F190B8396AF069D35D717BF(__this, NULL);
		NullCheck(L_0);
		RuntimeObject* L_1;
		L_1 = InterfaceFuncInvoker0< RuntimeObject* >::Invoke(0 /* System.Collections.Generic.IEnumerator`1<T> System.Collections.Generic.IEnumerable`1<UnityEngine.Vector2Int>::GetEnumerator() */, IEnumerable_1_t8845214D7CADFAAD7AB98132A368905184A79DDF_il2cpp_TypeInfo_var, L_0);
		V_1 = L_1;
	}
	{
		auto __finallyBlock = il2cpp::utils::Finally([&]
		{

FINALLY_0025:
			{// begin finally (depth: 1)
				{
					RuntimeObject* L_2 = V_1;
					if (!L_2)
					{
						goto IL_002e;
					}
				}
				{
					RuntimeObject* L_3 = V_1;
					NullCheck(L_3);
					InterfaceActionInvoker0::Invoke(0 /* System.Void System.IDisposable::Dispose() */, IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5_il2cpp_TypeInfo_var, L_3);
				}

IL_002e:
				{
					return;
				}
			}// end finally (depth: 1)
		});
		try
		{// begin try (depth: 1)
			{
				goto IL_001b_1;
			}

IL_0010_1:
			{
				// foreach (var _ in EnumerateOccupiedCells())
				RuntimeObject* L_4 = V_1;
				NullCheck(L_4);
				Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A L_5;
				L_5 = InterfaceFuncInvoker0< Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A >::Invoke(0 /* T System.Collections.Generic.IEnumerator`1<UnityEngine.Vector2Int>::get_Current() */, IEnumerator_1_t772680255A1A75379853D3823763B88BF026E055_il2cpp_TypeInfo_var, L_4);
				// n++;
				int32_t L_6 = V_0;
				V_0 = ((int32_t)il2cpp_codegen_add(L_6, 1));
			}

IL_001b_1:
			{
				// foreach (var _ in EnumerateOccupiedCells())
				RuntimeObject* L_7 = V_1;
				NullCheck(L_7);
				bool L_8;
				L_8 = InterfaceFuncInvoker0< bool >::Invoke(0 /* System.Boolean System.Collections.IEnumerator::MoveNext() */, IEnumerator_t7B609C2FFA6EB5167D9C62A0C32A21DE2F666DAA_il2cpp_TypeInfo_var, L_7);
				if (L_8)
				{
					goto IL_0010_1;
				}
			}
			{
				goto IL_002f;
			}
		}// end try (depth: 1)
		catch(Il2CppExceptionWrapper& e)
		{
			__finallyBlock.StoreException(e.ex);
		}
	}

IL_002f:
	{
		// return n;
		int32_t L_9 = V_0;
		return L_9;
	}
}
// UnityEngine.Vector3 CrowdMatch.WallItem::CellWorld(UnityEngine.Vector2)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 WallItem_CellWorld_mD3D389CFFC4517B5825563C76508F9245868E006 (WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* __this, Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 ___p0, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* V_0 = NULL;
	Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A V_1;
	memset((&V_1), 0, sizeof(V_1));
	{
		// var g = Group;
		PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* L_0;
		L_0 = WallItem_get_Group_m467F5ED6CCE4DFEFEBF4F86532BE7ED2CD614729(__this, NULL);
		V_0 = L_0;
		// if (g == null)
		PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* L_1 = V_0;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_2;
		L_2 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_1, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_2)
		{
			goto IL_0033;
		}
	}
	{
		// return transform.TransformPoint(new Vector3(p.x, 0f, -p.y));
		Transform_tB27202C6F4E36D225EE28A13E4D662BF99785DB1* L_3;
		L_3 = Component_get_transform_m2919A1D81931E6932C7F06D4C2F0AB8DDA9A5371(__this, NULL);
		Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 L_4 = ___p0;
		float L_5 = L_4.___x_0;
		Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 L_6 = ___p0;
		float L_7 = L_6.___y_1;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_8;
		memset((&L_8), 0, sizeof(L_8));
		Vector3__ctor_m376936E6B999EF1ECBE57D990A386303E2283DE0_inline((&L_8), L_5, (0.0f), ((-L_7)), /*hidden argument*/NULL);
		NullCheck(L_3);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_9;
		L_9 = Transform_TransformPoint_m05BFF013DB830D7BFE44A007703694AE1062EE44(L_3, L_8, NULL);
		return L_9;
	}

IL_0033:
	{
		// Vector2Int c = ToCell(p);
		Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 L_10 = ___p0;
		Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A L_11;
		L_11 = WallItem_ToCell_mFA83E7FB1FBAB3694E1F9640EC1E3988748B61C6(L_10, NULL);
		V_1 = L_11;
		// return g.GetWorldPosition(c.x, c.y);
		PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* L_12 = V_0;
		int32_t L_13;
		L_13 = Vector2Int_get_x_mA2CACB1B6E6B5AD0CCC32B2CD2EDCE3ECEB50576_inline((&V_1), NULL);
		int32_t L_14;
		L_14 = Vector2Int_get_y_m48454163ECF0B463FB5A16A0C4FC4B14DB0768B3_inline((&V_1), NULL);
		NullCheck(L_12);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_15;
		L_15 = PixelGroup_GetWorldPosition_m53DF201A615D1467AB8512D30C752A3826FBE608(L_12, L_13, L_14, NULL);
		return L_15;
	}
}
// System.Void CrowdMatch.WallItem::OnDrawGizmos()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void WallItem_OnDrawGizmos_m80E6D87DFA8A3973DAB5C61EB393404A0EB1E2DB (WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IEnumerable_1_t8845214D7CADFAAD7AB98132A368905184A79DDF_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IEnumerator_1_t772680255A1A75379853D3823763B88BF026E055_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IEnumerator_t7B609C2FFA6EB5167D9C62A0C32A21DE2F666DAA_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_get_Count_mF1C0C56457C655BDFFC6EE5B46FAD8BAEC1F588B_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_get_Item_m1F8E226CAD72B83C5E75BB66B43025247806B543_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* V_0 = NULL;
	Color_tD001788D726C3A7F1379BEED0260B9591F440C1F V_1;
	memset((&V_1), 0, sizeof(V_1));
	float V_2 = 0.0f;
	float V_3 = 0.0f;
	int32_t V_4 = 0;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 V_5;
	memset((&V_5), 0, sizeof(V_5));
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 V_6;
	memset((&V_6), 0, sizeof(V_6));
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 V_7;
	memset((&V_7), 0, sizeof(V_7));
	RuntimeObject* V_8 = NULL;
	Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A V_9;
	memset((&V_9), 0, sizeof(V_9));
	{
		// var g = Group;
		PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* L_0;
		L_0 = WallItem_get_Group_m467F5ED6CCE4DFEFEBF4F86532BE7ED2CD614729(__this, NULL);
		V_0 = L_0;
		// if (g == null)
		PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* L_1 = V_0;
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_2;
		L_2 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_1, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_2)
		{
			goto IL_0011;
		}
	}
	{
		// return;
		return;
	}

IL_0011:
	{
		// Gizmos.color = gizmoColor;
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_3 = __this->___gizmoColor_6;
		Gizmos_set_color_m53927A2741937484180B20B55F7F20F8F60C5797(L_3, NULL);
		// for (int i = 0; i + 1 < points.Count; i++)
		V_4 = 0;
		goto IL_00a6;
	}

IL_0024:
	{
		// Vector3 a = CellWorld(points[i]);
		List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B* L_4 = __this->___points_4;
		int32_t L_5 = V_4;
		NullCheck(L_4);
		Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 L_6;
		L_6 = List_1_get_Item_m1F8E226CAD72B83C5E75BB66B43025247806B543(L_4, L_5, List_1_get_Item_m1F8E226CAD72B83C5E75BB66B43025247806B543_RuntimeMethod_var);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_7;
		L_7 = WallItem_CellWorld_mD3D389CFFC4517B5825563C76508F9245868E006(__this, L_6, NULL);
		// Vector3 b = CellWorld(points[i + 1]);
		List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B* L_8 = __this->___points_4;
		int32_t L_9 = V_4;
		NullCheck(L_8);
		Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 L_10;
		L_10 = List_1_get_Item_m1F8E226CAD72B83C5E75BB66B43025247806B543(L_8, ((int32_t)il2cpp_codegen_add(L_9, 1)), List_1_get_Item_m1F8E226CAD72B83C5E75BB66B43025247806B543_RuntimeMethod_var);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_11;
		L_11 = WallItem_CellWorld_mD3D389CFFC4517B5825563C76508F9245868E006(__this, L_10, NULL);
		V_5 = L_11;
		// Vector3 aTop = a + Vector3.up * height;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_12 = L_7;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_13;
		L_13 = Vector3_get_up_m128AF3FDC820BF59D5DE86D973E7DE3F20C3AEBA_inline(NULL);
		float L_14 = __this->___height_5;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_15;
		L_15 = Vector3_op_Multiply_m87BA7C578F96C8E49BB07088DAAC4649F83B0353_inline(L_13, L_14, NULL);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_16;
		L_16 = Vector3_op_Addition_m78C0EC70CB66E8DCAC225743D82B268DAEE92067_inline(L_12, L_15, NULL);
		V_6 = L_16;
		// Vector3 bTop = b + Vector3.up * height;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_17 = V_5;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_18;
		L_18 = Vector3_get_up_m128AF3FDC820BF59D5DE86D973E7DE3F20C3AEBA_inline(NULL);
		float L_19 = __this->___height_5;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_20;
		L_20 = Vector3_op_Multiply_m87BA7C578F96C8E49BB07088DAAC4649F83B0353_inline(L_18, L_19, NULL);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_21;
		L_21 = Vector3_op_Addition_m78C0EC70CB66E8DCAC225743D82B268DAEE92067_inline(L_17, L_20, NULL);
		V_7 = L_21;
		// Gizmos.DrawLine(a, b);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_22 = L_12;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_23 = V_5;
		Gizmos_DrawLine_mB139054F55D615637A39A3127AADB16043387F8A(L_22, L_23, NULL);
		// Gizmos.DrawLine(aTop, bTop);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_24 = V_6;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_25 = V_7;
		Gizmos_DrawLine_mB139054F55D615637A39A3127AADB16043387F8A(L_24, L_25, NULL);
		// Gizmos.DrawLine(a, aTop);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_26 = V_6;
		Gizmos_DrawLine_mB139054F55D615637A39A3127AADB16043387F8A(L_22, L_26, NULL);
		// Gizmos.DrawLine(b, bTop);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_27 = V_5;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_28 = V_7;
		Gizmos_DrawLine_mB139054F55D615637A39A3127AADB16043387F8A(L_27, L_28, NULL);
		// for (int i = 0; i + 1 < points.Count; i++)
		int32_t L_29 = V_4;
		V_4 = ((int32_t)il2cpp_codegen_add(L_29, 1));
	}

IL_00a6:
	{
		// for (int i = 0; i + 1 < points.Count; i++)
		int32_t L_30 = V_4;
		List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B* L_31 = __this->___points_4;
		NullCheck(L_31);
		int32_t L_32;
		L_32 = List_1_get_Count_mF1C0C56457C655BDFFC6EE5B46FAD8BAEC1F588B_inline(L_31, List_1_get_Count_mF1C0C56457C655BDFFC6EE5B46FAD8BAEC1F588B_RuntimeMethod_var);
		if ((((int32_t)((int32_t)il2cpp_codegen_add(L_30, 1))) < ((int32_t)L_32)))
		{
			goto IL_0024;
		}
	}
	{
		// Color cellColor = gizmoColor;
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_33 = __this->___gizmoColor_6;
		V_1 = L_33;
		// cellColor.a = Mathf.Clamp01(gizmoColor.a * 0.4f);
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F* L_34 = (&__this->___gizmoColor_6);
		float L_35 = L_34->___a_3;
		float L_36;
		L_36 = Mathf_Clamp01_mA7E048DBDA832D399A581BE4D6DED9FA44CE0F14_inline(((float)il2cpp_codegen_multiply(L_35, (0.400000006f))), NULL);
		(&V_1)->___a_3 = L_36;
		// Gizmos.color = cellColor;
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_37 = V_1;
		Gizmos_set_color_m53927A2741937484180B20B55F7F20F8F60C5797(L_37, NULL);
		// float halfX = g.CellSizeX * 0.45f;
		PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* L_38 = V_0;
		NullCheck(L_38);
		float L_39;
		L_39 = PixelGroup_get_CellSizeX_mFF4EC1FE8D75565520456EB7BD12D9DFCF2AFAE0(L_38, NULL);
		V_2 = ((float)il2cpp_codegen_multiply(L_39, (0.449999988f)));
		// float halfZ = g.CellSizeZ * 0.45f;
		PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* L_40 = V_0;
		NullCheck(L_40);
		float L_41;
		L_41 = PixelGroup_get_CellSizeZ_m8BD98A189CFE63A89D5928CDE4EB4DD7D681CC50(L_40, NULL);
		V_3 = ((float)il2cpp_codegen_multiply(L_41, (0.449999988f)));
		// foreach (var cell in EnumerateOccupiedCells())
		RuntimeObject* L_42;
		L_42 = WallItem_EnumerateOccupiedCells_mA15C2F1FBAAE6D7E2F190B8396AF069D35D717BF(__this, NULL);
		NullCheck(L_42);
		RuntimeObject* L_43;
		L_43 = InterfaceFuncInvoker0< RuntimeObject* >::Invoke(0 /* System.Collections.Generic.IEnumerator`1<T> System.Collections.Generic.IEnumerable`1<UnityEngine.Vector2Int>::GetEnumerator() */, IEnumerable_1_t8845214D7CADFAAD7AB98132A368905184A79DDF_il2cpp_TypeInfo_var, L_42);
		V_8 = L_43;
	}
	{
		auto __finallyBlock = il2cpp::utils::Finally([&]
		{

FINALLY_0168:
			{// begin finally (depth: 1)
				{
					RuntimeObject* L_44 = V_8;
					if (!L_44)
					{
						goto IL_0173;
					}
				}
				{
					RuntimeObject* L_45 = V_8;
					NullCheck(L_45);
					InterfaceActionInvoker0::Invoke(0 /* System.Void System.IDisposable::Dispose() */, IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5_il2cpp_TypeInfo_var, L_45);
				}

IL_0173:
				{
					return;
				}
			}// end finally (depth: 1)
		});
		try
		{// begin try (depth: 1)
			{
				goto IL_015d_1;
			}

IL_010d_1:
			{
				// foreach (var cell in EnumerateOccupiedCells())
				RuntimeObject* L_46 = V_8;
				NullCheck(L_46);
				Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A L_47;
				L_47 = InterfaceFuncInvoker0< Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A >::Invoke(0 /* T System.Collections.Generic.IEnumerator`1<UnityEngine.Vector2Int>::get_Current() */, IEnumerator_1_t772680255A1A75379853D3823763B88BF026E055_il2cpp_TypeInfo_var, L_46);
				V_9 = L_47;
				// if (!g.IsInRange(cell.x, cell.y))
				PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* L_48 = V_0;
				int32_t L_49;
				L_49 = Vector2Int_get_x_mA2CACB1B6E6B5AD0CCC32B2CD2EDCE3ECEB50576_inline((&V_9), NULL);
				int32_t L_50;
				L_50 = Vector2Int_get_y_m48454163ECF0B463FB5A16A0C4FC4B14DB0768B3_inline((&V_9), NULL);
				NullCheck(L_48);
				bool L_51;
				L_51 = PixelGroup_IsInRange_m7D51A009EE60A9ADDC0707368ADDDE95FCF788C3(L_48, L_49, L_50, NULL);
				if (!L_51)
				{
					goto IL_015d_1;
				}
			}
			{
				// Vector3 center = g.GetWorldPosition(cell.x, cell.y);
				PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* L_52 = V_0;
				int32_t L_53;
				L_53 = Vector2Int_get_x_mA2CACB1B6E6B5AD0CCC32B2CD2EDCE3ECEB50576_inline((&V_9), NULL);
				int32_t L_54;
				L_54 = Vector2Int_get_y_m48454163ECF0B463FB5A16A0C4FC4B14DB0768B3_inline((&V_9), NULL);
				NullCheck(L_52);
				Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_55;
				L_55 = PixelGroup_GetWorldPosition_m53DF201A615D1467AB8512D30C752A3826FBE608(L_52, L_53, L_54, NULL);
				// Gizmos.DrawCube(center, new Vector3(halfX * 2f, 0.04f, halfZ * 2f));
				float L_56 = V_2;
				float L_57 = V_3;
				Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_58;
				memset((&L_58), 0, sizeof(L_58));
				Vector3__ctor_m376936E6B999EF1ECBE57D990A386303E2283DE0_inline((&L_58), ((float)il2cpp_codegen_multiply(L_56, (2.0f))), (0.0399999991f), ((float)il2cpp_codegen_multiply(L_57, (2.0f))), /*hidden argument*/NULL);
				Gizmos_DrawCube_m4417EAEA479EF4AD52445810D840BA8FCBC6EF3F(L_55, L_58, NULL);
			}

IL_015d_1:
			{
				// foreach (var cell in EnumerateOccupiedCells())
				RuntimeObject* L_59 = V_8;
				NullCheck(L_59);
				bool L_60;
				L_60 = InterfaceFuncInvoker0< bool >::Invoke(0 /* System.Boolean System.Collections.IEnumerator::MoveNext() */, IEnumerator_t7B609C2FFA6EB5167D9C62A0C32A21DE2F666DAA_il2cpp_TypeInfo_var, L_59);
				if (L_60)
				{
					goto IL_010d_1;
				}
			}
			{
				goto IL_0174;
			}
		}// end try (depth: 1)
		catch(Il2CppExceptionWrapper& e)
		{
			__finallyBlock.StoreException(e.ex);
		}
	}

IL_0174:
	{
		// }
		return;
	}
}
// System.Void CrowdMatch.WallItem::OnDrawGizmosSelected()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void WallItem_OnDrawGizmosSelected_m0FE89C3F75CC423948BED236385D152D45D89323 (WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_get_Count_mF1C0C56457C655BDFFC6EE5B46FAD8BAEC1F588B_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_get_Item_m1F8E226CAD72B83C5E75BB66B43025247806B543_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	Color_tD001788D726C3A7F1379BEED0260B9591F440C1F V_0;
	memset((&V_0), 0, sizeof(V_0));
	int32_t V_1 = 0;
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 V_2;
	memset((&V_2), 0, sizeof(V_2));
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 V_3;
	memset((&V_3), 0, sizeof(V_3));
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 V_4;
	memset((&V_4), 0, sizeof(V_4));
	{
		// var g = Group;
		PixelGroup_tD54EFA9929375FA39FEAEF3455304E3C4D131E80* L_0;
		L_0 = WallItem_get_Group_m467F5ED6CCE4DFEFEBF4F86532BE7ED2CD614729(__this, NULL);
		// if (g == null)
		il2cpp_codegen_runtime_class_init_inline(Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C_il2cpp_TypeInfo_var);
		bool L_1;
		L_1 = Object_op_Equality_mB6120F782D83091EF56A198FCEBCF066DB4A9605(L_0, (Object_tC12DECB6760A7F2CBF65D9DCF18D044C2D97152C*)NULL, NULL);
		if (!L_1)
		{
			goto IL_000f;
		}
	}
	{
		// return;
		return;
	}

IL_000f:
	{
		// Color prev = Gizmos.color;
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_2;
		L_2 = Gizmos_get_color_mF7A6194876F0DB8D2629715134BAAD3765849A3B(NULL);
		V_0 = L_2;
		// Gizmos.color = Color.white;
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_3;
		L_3 = Color_get_white_m068F5AF879B0FCA584E3693F762EA41BB65532C6_inline(NULL);
		Gizmos_set_color_m53927A2741937484180B20B55F7F20F8F60C5797(L_3, NULL);
		// for (int i = 0; i + 1 < points.Count; i++)
		V_1 = 0;
		goto IL_008b;
	}

IL_0023:
	{
		// Vector3 a = CellWorld(points[i]);
		List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B* L_4 = __this->___points_4;
		int32_t L_5 = V_1;
		NullCheck(L_4);
		Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 L_6;
		L_6 = List_1_get_Item_m1F8E226CAD72B83C5E75BB66B43025247806B543(L_4, L_5, List_1_get_Item_m1F8E226CAD72B83C5E75BB66B43025247806B543_RuntimeMethod_var);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_7;
		L_7 = WallItem_CellWorld_mD3D389CFFC4517B5825563C76508F9245868E006(__this, L_6, NULL);
		V_2 = L_7;
		// Vector3 b = CellWorld(points[i + 1]);
		List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B* L_8 = __this->___points_4;
		int32_t L_9 = V_1;
		NullCheck(L_8);
		Vector2_t1FD6F485C871E832B347AB2DC8CBA08B739D8DF7 L_10;
		L_10 = List_1_get_Item_m1F8E226CAD72B83C5E75BB66B43025247806B543(L_8, ((int32_t)il2cpp_codegen_add(L_9, 1)), List_1_get_Item_m1F8E226CAD72B83C5E75BB66B43025247806B543_RuntimeMethod_var);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_11;
		L_11 = WallItem_CellWorld_mD3D389CFFC4517B5825563C76508F9245868E006(__this, L_10, NULL);
		V_3 = L_11;
		// Vector3 aTop = a + Vector3.up * height;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_12 = V_2;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_13;
		L_13 = Vector3_get_up_m128AF3FDC820BF59D5DE86D973E7DE3F20C3AEBA_inline(NULL);
		float L_14 = __this->___height_5;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_15;
		L_15 = Vector3_op_Multiply_m87BA7C578F96C8E49BB07088DAAC4649F83B0353_inline(L_13, L_14, NULL);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_16;
		L_16 = Vector3_op_Addition_m78C0EC70CB66E8DCAC225743D82B268DAEE92067_inline(L_12, L_15, NULL);
		// Vector3 bTop = b + Vector3.up * height;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_17 = V_3;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_18;
		L_18 = Vector3_get_up_m128AF3FDC820BF59D5DE86D973E7DE3F20C3AEBA_inline(NULL);
		float L_19 = __this->___height_5;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_20;
		L_20 = Vector3_op_Multiply_m87BA7C578F96C8E49BB07088DAAC4649F83B0353_inline(L_18, L_19, NULL);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_21;
		L_21 = Vector3_op_Addition_m78C0EC70CB66E8DCAC225743D82B268DAEE92067_inline(L_17, L_20, NULL);
		V_4 = L_21;
		// Gizmos.DrawLine(a, b);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_22 = V_2;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_23 = V_3;
		Gizmos_DrawLine_mB139054F55D615637A39A3127AADB16043387F8A(L_22, L_23, NULL);
		// Gizmos.DrawLine(aTop, bTop);
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_24 = V_4;
		Gizmos_DrawLine_mB139054F55D615637A39A3127AADB16043387F8A(L_16, L_24, NULL);
		// for (int i = 0; i + 1 < points.Count; i++)
		int32_t L_25 = V_1;
		V_1 = ((int32_t)il2cpp_codegen_add(L_25, 1));
	}

IL_008b:
	{
		// for (int i = 0; i + 1 < points.Count; i++)
		int32_t L_26 = V_1;
		List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B* L_27 = __this->___points_4;
		NullCheck(L_27);
		int32_t L_28;
		L_28 = List_1_get_Count_mF1C0C56457C655BDFFC6EE5B46FAD8BAEC1F588B_inline(L_27, List_1_get_Count_mF1C0C56457C655BDFFC6EE5B46FAD8BAEC1F588B_RuntimeMethod_var);
		if ((((int32_t)((int32_t)il2cpp_codegen_add(L_26, 1))) < ((int32_t)L_28)))
		{
			goto IL_0023;
		}
	}
	{
		// Gizmos.color = prev;
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_29 = V_0;
		Gizmos_set_color_m53927A2741937484180B20B55F7F20F8F60C5797(L_29, NULL);
		// }
		return;
	}
}
// System.Void CrowdMatch.WallItem::.ctor()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void WallItem__ctor_m476542078A29E3090E903F8189BBA6B64ABB1CCB (WallItem_t777206F2380889EF324A1D7168EAE0DC5D4AD6E0* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1__ctor_m88C4BD8AC607DB3585552068F4DC437406358D5F_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		// public List<Vector2> points = new List<Vector2>();
		List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B* L_0 = (List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B*)il2cpp_codegen_object_new(List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B_il2cpp_TypeInfo_var);
		NullCheck(L_0);
		List_1__ctor_m88C4BD8AC607DB3585552068F4DC437406358D5F(L_0, List_1__ctor_m88C4BD8AC607DB3585552068F4DC437406358D5F_RuntimeMethod_var);
		__this->___points_4 = L_0;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___points_4), (void*)L_0);
		// public float height = 2f;
		__this->___height_5 = (2.0f);
		// public Color gizmoColor = new Color(1f, 0.35f, 0.35f, 0.65f);
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_1;
		memset((&L_1), 0, sizeof(L_1));
		Color__ctor_m3786F0D6E510D9CFA544523A955870BD2A514C8C_inline((&L_1), (1.0f), (0.349999994f), (0.349999994f), (0.649999976f), /*hidden argument*/NULL);
		__this->___gizmoColor_6 = L_1;
		MonoBehaviour__ctor_m592DB0105CA0BC97AA1C5F4AD27B12D68A3B7C1E(__this, NULL);
		return;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float Vector3_Distance_m2314DB9B8BD01157E013DF87BEA557375C7F9FF9_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___a0, Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___b1, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	float V_0 = 0.0f;
	float V_1 = 0.0f;
	float V_2 = 0.0f;
	float V_3 = 0.0f;
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_0 = ___a0;
		float L_1 = L_0.___x_2;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_2 = ___b1;
		float L_3 = L_2.___x_2;
		V_0 = ((float)il2cpp_codegen_subtract(L_1, L_3));
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_4 = ___a0;
		float L_5 = L_4.___y_3;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_6 = ___b1;
		float L_7 = L_6.___y_3;
		V_1 = ((float)il2cpp_codegen_subtract(L_5, L_7));
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_8 = ___a0;
		float L_9 = L_8.___z_4;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_10 = ___b1;
		float L_11 = L_10.___z_4;
		V_2 = ((float)il2cpp_codegen_subtract(L_9, L_11));
		float L_12 = V_0;
		float L_13 = V_0;
		float L_14 = V_1;
		float L_15 = V_1;
		float L_16 = V_2;
		float L_17 = V_2;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_18;
		L_18 = sqrt(((double)((float)il2cpp_codegen_add(((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(L_12, L_13)), ((float)il2cpp_codegen_multiply(L_14, L_15)))), ((float)il2cpp_codegen_multiply(L_16, L_17))))));
		V_3 = ((float)L_18);
		goto IL_0040;
	}

IL_0040:
	{
		float L_19 = V_3;
		return L_19;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float Mathf_Clamp01_mA7E048DBDA832D399A581BE4D6DED9FA44CE0F14_inline (float ___value0, const RuntimeMethod* method) 
{
	bool V_0 = false;
	float V_1 = 0.0f;
	bool V_2 = false;
	{
		float L_0 = ___value0;
		V_0 = (bool)((((float)L_0) < ((float)(0.0f)))? 1 : 0);
		bool L_1 = V_0;
		if (!L_1)
		{
			goto IL_0015;
		}
	}
	{
		V_1 = (0.0f);
		goto IL_002d;
	}

IL_0015:
	{
		float L_2 = ___value0;
		V_2 = (bool)((((float)L_2) > ((float)(1.0f)))? 1 : 0);
		bool L_3 = V_2;
		if (!L_3)
		{
			goto IL_0029;
		}
	}
	{
		V_1 = (1.0f);
		goto IL_002d;
	}

IL_0029:
	{
		float L_4 = ___value0;
		V_1 = L_4;
		goto IL_002d;
	}

IL_002d:
	{
		float L_5 = V_1;
		return L_5;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_Lerp_m3A906D0530A94FAABB94F0F905E84D99BE85C3F8_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___a0, Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___b1, float ___t2, const RuntimeMethod* method) 
{
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 V_0;
	memset((&V_0), 0, sizeof(V_0));
	{
		float L_0 = ___t2;
		float L_1;
		L_1 = Mathf_Clamp01_mA7E048DBDA832D399A581BE4D6DED9FA44CE0F14_inline(L_0, NULL);
		___t2 = L_1;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_2 = ___a0;
		float L_3 = L_2.___x_2;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_4 = ___b1;
		float L_5 = L_4.___x_2;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_6 = ___a0;
		float L_7 = L_6.___x_2;
		float L_8 = ___t2;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_9 = ___a0;
		float L_10 = L_9.___y_3;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_11 = ___b1;
		float L_12 = L_11.___y_3;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_13 = ___a0;
		float L_14 = L_13.___y_3;
		float L_15 = ___t2;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_16 = ___a0;
		float L_17 = L_16.___z_4;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_18 = ___b1;
		float L_19 = L_18.___z_4;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_20 = ___a0;
		float L_21 = L_20.___z_4;
		float L_22 = ___t2;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_23;
		memset((&L_23), 0, sizeof(L_23));
		Vector3__ctor_m376936E6B999EF1ECBE57D990A386303E2283DE0_inline((&L_23), ((float)il2cpp_codegen_add(L_3, ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_subtract(L_5, L_7)), L_8)))), ((float)il2cpp_codegen_add(L_10, ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_subtract(L_12, L_14)), L_15)))), ((float)il2cpp_codegen_add(L_17, ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_subtract(L_19, L_21)), L_22)))), /*hidden argument*/NULL);
		V_0 = L_23;
		goto IL_0053;
	}

IL_0053:
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_24 = V_0;
		return L_24;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR bool Mathf_Approximately_m1DADD012A8FC82E11FB282501AE2EBBF9A77150B_inline (float ___a0, float ___b1, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Mathf_tE284D016E3B297B72311AAD9EB8F0E643F6A4682_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	bool V_0 = false;
	{
		float L_0 = ___b1;
		float L_1 = ___a0;
		float L_2;
		L_2 = fabsf(((float)il2cpp_codegen_subtract(L_0, L_1)));
		float L_3 = ___a0;
		float L_4;
		L_4 = fabsf(L_3);
		float L_5 = ___b1;
		float L_6;
		L_6 = fabsf(L_5);
		float L_7;
		L_7 = Mathf_Max_mF5379E63D2BBAC76D090748695D833934F8AD051_inline(L_4, L_6, NULL);
		float L_8 = ((Mathf_tE284D016E3B297B72311AAD9EB8F0E643F6A4682_StaticFields*)il2cpp_codegen_static_fields_for(Mathf_tE284D016E3B297B72311AAD9EB8F0E643F6A4682_il2cpp_TypeInfo_var))->___Epsilon_0;
		float L_9;
		L_9 = Mathf_Max_mF5379E63D2BBAC76D090748695D833934F8AD051_inline(((float)il2cpp_codegen_multiply((9.99999997E-07f), L_7)), ((float)il2cpp_codegen_multiply(L_8, (8.0f))), NULL);
		V_0 = (bool)((((float)L_2) < ((float)L_9))? 1 : 0);
		goto IL_0035;
	}

IL_0035:
	{
		bool L_10 = V_0;
		return L_10;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR ButtonClickedEvent_t8EA72E90B3BD1392FB3B3EF167D5121C23569E4C* Button_get_onClick_m701712A7F7F000CC80D517C4510697E15722C35C_inline (Button_t6786514A57F7AFDEE5431112FEA0CAB24F5AE098* __this, const RuntimeMethod* method) 
{
	{
		// get { return m_OnClick; }
		ButtonClickedEvent_t8EA72E90B3BD1392FB3B3EF167D5121C23569E4C* L_0 = __this->___m_OnClick_20;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR GameManager_t5B23045F478AD8B0E3D943ECC8493384B02FF32F* GameManager_get_Instance_m5F7736EF916BFD34C734BE27B0EA4760C2D545FA_inline (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&GameManager_t5B23045F478AD8B0E3D943ECC8493384B02FF32F_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	{
		// public static GameManager Instance { get; private set; }
		GameManager_t5B23045F478AD8B0E3D943ECC8493384B02FF32F* L_0 = ((GameManager_t5B23045F478AD8B0E3D943ECC8493384B02FF32F_StaticFields*)il2cpp_codegen_static_fields_for(GameManager_t5B23045F478AD8B0E3D943ECC8493384B02FF32F_il2cpp_TypeInfo_var))->___U3CInstanceU3Ek__BackingField_4;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void PixelClickListener_set_BoxCollider_m103128AD33A18949AC8C92230E2451E44A68D847_inline (PixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524* __this, BoxCollider_tFA5D239388334D6DE0B8FFDAD6825C5B03786E23* ___value0, const RuntimeMethod* method) 
{
	{
		// public BoxCollider BoxCollider { get; private set; }
		BoxCollider_tFA5D239388334D6DE0B8FFDAD6825C5B03786E23* L_0 = ___value0;
		__this->___U3CBoxColliderU3Ek__BackingField_5 = L_0;
		Il2CppCodeGenWriteBarrier((void**)(&__this->___U3CBoxColliderU3Ek__BackingField_5), (void*)L_0);
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR BoxCollider_tFA5D239388334D6DE0B8FFDAD6825C5B03786E23* PixelClickListener_get_BoxCollider_m9BC4090DD0238F70A3BD78FA280683C138C66C63_inline (PixelClickListener_t1FAA47270E43D45FC3B995B80629279B262F2524* __this, const RuntimeMethod* method) 
{
	{
		// public BoxCollider BoxCollider { get; private set; }
		BoxCollider_tFA5D239388334D6DE0B8FFDAD6825C5B03786E23* L_0 = __this->___U3CBoxColliderU3Ek__BackingField_5;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t Mathf_Max_m7FA442918DE37E3A00106D1F2E789D65829792B8_inline (int32_t ___a0, int32_t ___b1, const RuntimeMethod* method) 
{
	int32_t V_0 = 0;
	int32_t G_B3_0 = 0;
	{
		int32_t L_0 = ___a0;
		int32_t L_1 = ___b1;
		if ((((int32_t)L_0) > ((int32_t)L_1)))
		{
			goto IL_0008;
		}
	}
	{
		int32_t L_2 = ___b1;
		G_B3_0 = L_2;
		goto IL_0009;
	}

IL_0008:
	{
		int32_t L_3 = ___a0;
		G_B3_0 = L_3;
	}

IL_0009:
	{
		V_0 = G_B3_0;
		goto IL_000c;
	}

IL_000c:
	{
		int32_t L_4 = V_0;
		return L_4;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t Vector2Int_get_x_mA2CACB1B6E6B5AD0CCC32B2CD2EDCE3ECEB50576_inline (Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A* __this, const RuntimeMethod* method) 
{
	int32_t V_0 = 0;
	{
		int32_t L_0 = __this->___m_X_0;
		V_0 = L_0;
		goto IL_000a;
	}

IL_000a:
	{
		int32_t L_1 = V_0;
		return L_1;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t Vector2Int_get_y_m48454163ECF0B463FB5A16A0C4FC4B14DB0768B3_inline (Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A* __this, const RuntimeMethod* method) 
{
	int32_t V_0 = 0;
	{
		int32_t L_0 = __this->___m_Y_1;
		V_0 = L_0;
		goto IL_000a;
	}

IL_000a:
	{
		int32_t L_1 = V_0;
		return L_1;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Vector3__ctor_m376936E6B999EF1ECBE57D990A386303E2283DE0_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2* __this, float ___x0, float ___y1, float ___z2, const RuntimeMethod* method) 
{
	{
		float L_0 = ___x0;
		__this->___x_2 = L_0;
		float L_1 = ___y1;
		__this->___y_3 = L_1;
		float L_2 = ___z2;
		__this->___z_4 = L_2;
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Vector2Int__ctor_mC20D1312133EB8CB63EC11067088B043660F11CE_inline (Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A* __this, int32_t ___x0, int32_t ___y1, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = ___x0;
		__this->___m_X_0 = L_0;
		int32_t L_1 = ___y1;
		__this->___m_Y_1 = L_1;
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_get_zero_m0C1249C3F25B1C70EAD3CC8B31259975A457AE39_inline (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 V_0;
	memset((&V_0), 0, sizeof(V_0));
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_0 = ((Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2_StaticFields*)il2cpp_codegen_static_fields_for(Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2_il2cpp_TypeInfo_var))->___zeroVector_5;
		V_0 = L_0;
		goto IL_0009;
	}

IL_0009:
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_1 = V_0;
		return L_1;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_get_one_mC9B289F1E15C42C597180C9FE6FB492495B51D02_inline (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 V_0;
	memset((&V_0), 0, sizeof(V_0));
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_0 = ((Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2_StaticFields*)il2cpp_codegen_static_fields_for(Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2_il2cpp_TypeInfo_var))->___oneVector_6;
		V_0 = L_0;
		goto IL_0009;
	}

IL_0009:
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_1 = V_0;
		return L_1;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_op_Multiply_m87BA7C578F96C8E49BB07088DAAC4649F83B0353_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___a0, float ___d1, const RuntimeMethod* method) 
{
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 V_0;
	memset((&V_0), 0, sizeof(V_0));
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_0 = ___a0;
		float L_1 = L_0.___x_2;
		float L_2 = ___d1;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_3 = ___a0;
		float L_4 = L_3.___y_3;
		float L_5 = ___d1;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_6 = ___a0;
		float L_7 = L_6.___z_4;
		float L_8 = ___d1;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_9;
		memset((&L_9), 0, sizeof(L_9));
		Vector3__ctor_m376936E6B999EF1ECBE57D990A386303E2283DE0_inline((&L_9), ((float)il2cpp_codegen_multiply(L_1, L_2)), ((float)il2cpp_codegen_multiply(L_4, L_5)), ((float)il2cpp_codegen_multiply(L_7, L_8)), /*hidden argument*/NULL);
		V_0 = L_9;
		goto IL_0021;
	}

IL_0021:
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_10 = V_0;
		return L_10;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 Quaternion_get_identity_m7E701AE095ED10FD5EA0B50ABCFDE2EEFF2173A5_inline (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 V_0;
	memset((&V_0), 0, sizeof(V_0));
	{
		Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 L_0 = ((Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974_StaticFields*)il2cpp_codegen_static_fields_for(Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974_il2cpp_TypeInfo_var))->___identityQuaternion_4;
		V_0 = L_0;
		goto IL_0009;
	}

IL_0009:
	{
		Quaternion_tDA59F214EF07D7700B26E40E562F267AF7306974 L_1 = V_0;
		return L_1;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR bool PixelItem_get_IsExposed_mC91D7897C85B411BACD2B7CA57A5D9473985125B_inline (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, const RuntimeMethod* method) 
{
	{
		// public bool IsExposed { get; private set; }
		bool L_0 = __this->___U3CIsExposedU3Ek__BackingField_12;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void PixelItem_set_IsExposed_m7C2512B8A1E4DEB1D563791BDFB493BF5C98DBCB_inline (PixelItem_t863890C77945A8C08435BF2F8859A47A413C794E* __this, bool ___value0, const RuntimeMethod* method) 
{
	{
		// public bool IsExposed { get; private set; }
		bool L_0 = ___value0;
		__this->___U3CIsExposedU3Ek__BackingField_12 = L_0;
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float Mathf_Max_mF5379E63D2BBAC76D090748695D833934F8AD051_inline (float ___a0, float ___b1, const RuntimeMethod* method) 
{
	float V_0 = 0.0f;
	float G_B3_0 = 0.0f;
	{
		float L_0 = ___a0;
		float L_1 = ___b1;
		if ((((float)L_0) > ((float)L_1)))
		{
			goto IL_0008;
		}
	}
	{
		float L_2 = ___b1;
		G_B3_0 = L_2;
		goto IL_0009;
	}

IL_0008:
	{
		float L_3 = ___a0;
		G_B3_0 = L_3;
	}

IL_0009:
	{
		V_0 = G_B3_0;
		goto IL_000c;
	}

IL_000c:
	{
		float L_4 = V_0;
		return L_4;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t Mathf_Clamp_m4DC36EEFDBE5F07C16249DA568023C5ECCFF0E7B_inline (int32_t ___value0, int32_t ___min1, int32_t ___max2, const RuntimeMethod* method) 
{
	bool V_0 = false;
	bool V_1 = false;
	int32_t V_2 = 0;
	{
		int32_t L_0 = ___value0;
		int32_t L_1 = ___min1;
		V_0 = (bool)((((int32_t)L_0) < ((int32_t)L_1))? 1 : 0);
		bool L_2 = V_0;
		if (!L_2)
		{
			goto IL_000e;
		}
	}
	{
		int32_t L_3 = ___min1;
		___value0 = L_3;
		goto IL_0019;
	}

IL_000e:
	{
		int32_t L_4 = ___value0;
		int32_t L_5 = ___max2;
		V_1 = (bool)((((int32_t)L_4) > ((int32_t)L_5))? 1 : 0);
		bool L_6 = V_1;
		if (!L_6)
		{
			goto IL_0019;
		}
	}
	{
		int32_t L_7 = ___max2;
		___value0 = L_7;
	}

IL_0019:
	{
		int32_t L_8 = ___value0;
		V_2 = L_8;
		goto IL_001d;
	}

IL_001d:
	{
		int32_t L_9 = V_2;
		return L_9;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Color_tD001788D726C3A7F1379BEED0260B9591F440C1F Color_get_white_m068F5AF879B0FCA584E3693F762EA41BB65532C6_inline (const RuntimeMethod* method) 
{
	Color_tD001788D726C3A7F1379BEED0260B9591F440C1F V_0;
	memset((&V_0), 0, sizeof(V_0));
	{
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_0;
		memset((&L_0), 0, sizeof(L_0));
		Color__ctor_m3786F0D6E510D9CFA544523A955870BD2A514C8C_inline((&L_0), (1.0f), (1.0f), (1.0f), (1.0f), /*hidden argument*/NULL);
		V_0 = L_0;
		goto IL_001d;
	}

IL_001d:
	{
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_1 = V_0;
		return L_1;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t Mathf_RoundToInt_m60F8B66CF27F1FA75AA219342BD184B75771EB4B_inline (float ___f0, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	{
		float L_0 = ___f0;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_1;
		L_1 = bankers_round(((double)L_0));
		V_0 = il2cpp_codegen_cast_double_to_int<int32_t>(L_1);
		goto IL_000c;
	}

IL_000c:
	{
		int32_t L_2 = V_0;
		return L_2;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t Mathf_Abs_mD945EDDEA0D62D21BFDBAB7B1C0F18DFF1CEC905_inline (int32_t ___value0, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	{
		int32_t L_0 = ___value0;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		int32_t L_1;
		L_1 = il2cpp_codegen_abs(L_0);
		V_0 = L_1;
		goto IL_000a;
	}

IL_000a:
	{
		int32_t L_2 = V_0;
		return L_2;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_get_up_m128AF3FDC820BF59D5DE86D973E7DE3F20C3AEBA_inline (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 V_0;
	memset((&V_0), 0, sizeof(V_0));
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_0 = ((Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2_StaticFields*)il2cpp_codegen_static_fields_for(Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2_il2cpp_TypeInfo_var))->___upVector_7;
		V_0 = L_0;
		goto IL_0009;
	}

IL_0009:
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_1 = V_0;
		return L_1;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 Vector3_op_Addition_m78C0EC70CB66E8DCAC225743D82B268DAEE92067_inline (Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___a0, Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 ___b1, const RuntimeMethod* method) 
{
	Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 V_0;
	memset((&V_0), 0, sizeof(V_0));
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_0 = ___a0;
		float L_1 = L_0.___x_2;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_2 = ___b1;
		float L_3 = L_2.___x_2;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_4 = ___a0;
		float L_5 = L_4.___y_3;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_6 = ___b1;
		float L_7 = L_6.___y_3;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_8 = ___a0;
		float L_9 = L_8.___z_4;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_10 = ___b1;
		float L_11 = L_10.___z_4;
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_12;
		memset((&L_12), 0, sizeof(L_12));
		Vector3__ctor_m376936E6B999EF1ECBE57D990A386303E2283DE0_inline((&L_12), ((float)il2cpp_codegen_add(L_1, L_3)), ((float)il2cpp_codegen_add(L_5, L_7)), ((float)il2cpp_codegen_add(L_9, L_11)), /*hidden argument*/NULL);
		V_0 = L_12;
		goto IL_0030;
	}

IL_0030:
	{
		Vector3_t24C512C7B96BBABAD472002D0BA2BDA40A5A80B2 L_13 = V_0;
		return L_13;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void Color__ctor_m3786F0D6E510D9CFA544523A955870BD2A514C8C_inline (Color_tD001788D726C3A7F1379BEED0260B9591F440C1F* __this, float ___r0, float ___g1, float ___b2, float ___a3, const RuntimeMethod* method) 
{
	{
		float L_0 = ___r0;
		__this->___r_0 = L_0;
		float L_1 = ___g1;
		__this->___g_1 = L_1;
		float L_2 = ___b2;
		__this->___b_2 = L_2;
		float L_3 = ___a3;
		__this->___a_3 = L_3;
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void List_1_Add_m771AC7A01DFC931CCCFCCF949C1F4D56B5E98A1B_gshared_inline (List_1_tB56F1028A724D2CE4E84861619D1CF68C68C983D* __this, Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A ___item0, const RuntimeMethod* method) 
{
	Vector2IntU5BU5D_tF9E2BDAC11B246DF7EEB9137B826A0CBEBD59534* V_0 = NULL;
	int32_t V_1 = 0;
	{
		int32_t L_0 = (int32_t)__this->____version_3;
		__this->____version_3 = ((int32_t)il2cpp_codegen_add(L_0, 1));
		Vector2IntU5BU5D_tF9E2BDAC11B246DF7EEB9137B826A0CBEBD59534* L_1 = (Vector2IntU5BU5D_tF9E2BDAC11B246DF7EEB9137B826A0CBEBD59534*)__this->____items_1;
		V_0 = L_1;
		int32_t L_2 = (int32_t)__this->____size_2;
		V_1 = L_2;
		int32_t L_3 = V_1;
		Vector2IntU5BU5D_tF9E2BDAC11B246DF7EEB9137B826A0CBEBD59534* L_4 = V_0;
		NullCheck(L_4);
		if ((!(((uint32_t)L_3) < ((uint32_t)((int32_t)(((RuntimeArray*)L_4)->max_length))))))
		{
			goto IL_0034;
		}
	}
	{
		int32_t L_5 = V_1;
		__this->____size_2 = ((int32_t)il2cpp_codegen_add(L_5, 1));
		Vector2IntU5BU5D_tF9E2BDAC11B246DF7EEB9137B826A0CBEBD59534* L_6 = V_0;
		int32_t L_7 = V_1;
		Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A L_8 = ___item0;
		NullCheck(L_6);
		(L_6)->SetAt(static_cast<il2cpp_array_size_t>(L_7), (Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A)L_8);
		return;
	}

IL_0034:
	{
		Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A L_9 = ___item0;
		((  void (*) (List_1_tB56F1028A724D2CE4E84861619D1CF68C68C983D*, Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A, const RuntimeMethod*))il2cpp_codegen_get_method_pointer(il2cpp_rgctx_method(method->klass->rgctx_data, 11)))(__this, L_9, il2cpp_rgctx_method(method->klass->rgctx_data, 11));
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t Queue_1_get_Count_m1FE2DD00C23DF83E6DBC5BE6A23A9FE7FBF772DD_gshared_inline (Queue_1_t9B96DF949ACABD4B0F595738FC87DC80BCCC3875* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = (int32_t)__this->____size_3;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A Enumerator_get_Current_m87245A61572727EBCD1642C4A2BD99B11CE9FA8A_gshared_inline (Enumerator_t4BD1B5394A2E9960562CC364C2CCE4FCBD1AC258* __this, const RuntimeMethod* method) 
{
	{
		Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A L_0 = (Vector2Int_t69B2886EBAB732D9B880565E18E7568F3DE0CE6A)__this->____current_3;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR RuntimeObject* Enumerator_get_Current_m6330F15D18EE4F547C05DF9BF83C5EB710376027_gshared_inline (Enumerator_t9473BAB568A27E2339D48C1F91319E0F6D244D7A* __this, const RuntimeMethod* method) 
{
	{
		RuntimeObject* L_0 = (RuntimeObject*)__this->____current_3;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR bool Func_3_Invoke_m5C4CCADFF1AE4540F252182089A9BF3CBE7BAFE6_gshared_inline (Func_3_tE8F85DA3CAC4998201E5C56356280AFAB7185B69* __this, int32_t ___arg10, int32_t ___arg21, const RuntimeMethod* method) 
{
	typedef bool (*FunctionPointerType) (RuntimeObject*, int32_t, int32_t, const RuntimeMethod*);
	return ((FunctionPointerType)__this->___invoke_impl_1)((Il2CppObject*)__this->___method_code_6, ___arg10, ___arg21, reinterpret_cast<RuntimeMethod*>(__this->___method_3));
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR void List_1_Add_m716377944B1E88CBB1B269AA9CF38C525A41D367_gshared_inline (List_1_t264DA9C475A0E3CF57148457209560CF2E2B6F27* __this, ValueTuple_3_tBFE24970AFB182B3C77F4C747E3F1E7A7B4EBC57 ___item0, const RuntimeMethod* method) 
{
	ValueTuple_3U5BU5D_t2EE684CE383C822F20AF09A2B0A3C10C5A8A94E5* V_0 = NULL;
	int32_t V_1 = 0;
	{
		int32_t L_0 = (int32_t)__this->____version_3;
		__this->____version_3 = ((int32_t)il2cpp_codegen_add(L_0, 1));
		ValueTuple_3U5BU5D_t2EE684CE383C822F20AF09A2B0A3C10C5A8A94E5* L_1 = (ValueTuple_3U5BU5D_t2EE684CE383C822F20AF09A2B0A3C10C5A8A94E5*)__this->____items_1;
		V_0 = L_1;
		int32_t L_2 = (int32_t)__this->____size_2;
		V_1 = L_2;
		int32_t L_3 = V_1;
		ValueTuple_3U5BU5D_t2EE684CE383C822F20AF09A2B0A3C10C5A8A94E5* L_4 = V_0;
		NullCheck(L_4);
		if ((!(((uint32_t)L_3) < ((uint32_t)((int32_t)(((RuntimeArray*)L_4)->max_length))))))
		{
			goto IL_0034;
		}
	}
	{
		int32_t L_5 = V_1;
		__this->____size_2 = ((int32_t)il2cpp_codegen_add(L_5, 1));
		ValueTuple_3U5BU5D_t2EE684CE383C822F20AF09A2B0A3C10C5A8A94E5* L_6 = V_0;
		int32_t L_7 = V_1;
		ValueTuple_3_tBFE24970AFB182B3C77F4C747E3F1E7A7B4EBC57 L_8 = ___item0;
		NullCheck(L_6);
		(L_6)->SetAt(static_cast<il2cpp_array_size_t>(L_7), (ValueTuple_3_tBFE24970AFB182B3C77F4C747E3F1E7A7B4EBC57)L_8);
		return;
	}

IL_0034:
	{
		ValueTuple_3_tBFE24970AFB182B3C77F4C747E3F1E7A7B4EBC57 L_9 = ___item0;
		((  void (*) (List_1_t264DA9C475A0E3CF57148457209560CF2E2B6F27*, ValueTuple_3_tBFE24970AFB182B3C77F4C747E3F1E7A7B4EBC57, const RuntimeMethod*))il2cpp_codegen_get_method_pointer(il2cpp_rgctx_method(method->klass->rgctx_data, 11)))(__this, L_9, il2cpp_rgctx_method(method->klass->rgctx_data, 11));
		return;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR bool Nullable_1_get_HasValue_mFAF0B4EEA878E596C80258FE3BDA57CEF40C8D7F_gshared_inline (Nullable_1_tEE83D90B507D40B6C58B5EEF5B9D44D377B44F11* __this, const RuntimeMethod* method) 
{
	{
		bool L_0 = (bool)__this->___hasValue_0;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Color_tD001788D726C3A7F1379BEED0260B9591F440C1F Nullable_1_GetValueOrDefault_m9A7869C021F041D45F2A851F70F97F8114AC99E4_gshared_inline (Nullable_1_tEE83D90B507D40B6C58B5EEF5B9D44D377B44F11* __this, const RuntimeMethod* method) 
{
	{
		Color_tD001788D726C3A7F1379BEED0260B9591F440C1F L_0 = (Color_tD001788D726C3A7F1379BEED0260B9591F440C1F)__this->___value_1;
		return L_0;
	}
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR Nullable_1_tEE83D90B507D40B6C58B5EEF5B9D44D377B44F11 Func_3_Invoke_mADC4326AE0426011BAE02568945B03277B225B79_gshared_inline (Func_3_t171A20B65D1078C91C9A875EF6CBB2716C7E322D* __this, int32_t ___arg10, int32_t ___arg21, const RuntimeMethod* method) 
{
	typedef Nullable_1_tEE83D90B507D40B6C58B5EEF5B9D44D377B44F11 (*FunctionPointerType) (RuntimeObject*, int32_t, int32_t, const RuntimeMethod*);
	return ((FunctionPointerType)__this->___invoke_impl_1)((Il2CppObject*)__this->___method_code_6, ___arg10, ___arg21, reinterpret_cast<RuntimeMethod*>(__this->___method_3));
}
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t List_1_get_Count_mF1C0C56457C655BDFFC6EE5B46FAD8BAEC1F588B_gshared_inline (List_1_t8F3790B7F8C471B3A1336522C7415FB0AC36D47B* __this, const RuntimeMethod* method) 
{
	{
		int32_t L_0 = (int32_t)__this->____size_2;
		return L_0;
	}
}
