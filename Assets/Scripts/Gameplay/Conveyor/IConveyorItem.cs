using UnityEngine;

namespace CrowdMatch
{
    /// <summary>
    /// 传送带可承载的物体抽象接口。
    /// 业务端实现此接口，暴露 Transform 供传送带定位。
    /// Abstract item interface for objects carried by a ConveyorBelt.
    /// The host implements this interface and exposes a Transform for positioning.
    /// </summary>
    public interface IConveyorItem
    {
        /// <summary>
        /// 供传送带定位的 Transform。
        /// The Transform the conveyor uses for positioning.
        /// </summary>
        Transform Transform { get; }
    }
}
