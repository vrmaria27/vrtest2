using UnityEngine;

namespace Chelsea.XR
{
    // SDK isolation contracts. The controller developer adapter remains unverified on device.
    public interface IXrSession
    {
        bool HasFocus { get; }
        bool IsTracking { get; }
        bool SupportsPassthrough { get; }
        bool TrySetPassthrough(bool enabled);
    }

    public interface IWorldPlacement
    {
        bool TryPlace(Pose pose, float tabletopWidthMetres);
        void ReturnToTabletop();
    }

    // A later adapter must normalize hand/controller input to the same actions.
    public enum ExplorationAction
    {
        SelectLocation,
        GrabWorld,
        OpenMenu,
        Teleport,
        SnapTurnLeft,
        SnapTurnRight,
        ReturnToTabletop
    }

    public interface IExplorationInput
    {
        bool TryConsume(ExplorationAction action);
        bool TryGetSelectionRay(out Ray ray);
    }
}
