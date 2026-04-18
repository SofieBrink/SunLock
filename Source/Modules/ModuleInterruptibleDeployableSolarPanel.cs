/*
    Usecase:        Manually stopping and overriding automatic tracking of deployable solar panels.
    Example:        Artemis Construction Kit Orion tracking panels.
*/
using KSP.Localization;
using UnityEngine;

namespace SunLock.Modules
{
    class ModuleInterruptibleDeployableSolarPanel : ModuleDeployableSolarPanel
    {
        [KSPField(
            isPersistant = true,
            guiActive = true,
            guiActiveEditor = false,
            guiName = "#Sunlock_InterruptTracking"
        ),
        UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool interruptTracking = false;

        [KSPField(
            isPersistant = true,
            guiActive = true,
            guiActiveEditor = false,
            guiName = "#Sunlock_PanelAngle",
            guiFormat = "F1",
            guiUnits = "°"
        ),
        UI_FloatRange(minValue = -180f, maxValue = 180f, stepIncrement = 0.1f)]
        public float interruptAngle = 0f;

        private bool lastInterruptTracking = false;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            Fields[nameof(interruptTracking)].guiActive = ShouldShowInterrupt();
            Fields[nameof(interruptAngle)].guiActive = interruptTracking && ShouldShowInterrupt();

            lastInterruptTracking = interruptTracking;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            Fields[nameof(interruptTracking)].guiActive = ShouldShowInterrupt();
            Fields[nameof(interruptAngle)].guiActive = interruptTracking && ShouldShowInterrupt();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (interruptTracking && !lastInterruptTracking)
            {
                if (panelRotationTransform != null)
                {
                    Quaternion delta = Quaternion.Inverse(originalRotation) * panelRotationTransform.localRotation;
                    float angle = delta.eulerAngles.y;
                    if (angle > 180f) angle -= 360f;

                    interruptAngle = angle;
                }
            }

            lastInterruptTracking = interruptTracking;

            Fields[nameof(interruptTracking)].guiActive = ShouldShowInterrupt();
            Fields[nameof(interruptAngle)].guiActive = interruptTracking && ShouldShowInterrupt();
        }

        public override void CalculateTracking()
        {
            if (!HighLogic.LoadedSceneIsFlight || panelRotationTransform == null)
                return;

            if (deployState != DeployState.EXTENDED)
                return;

            Vector3 normalized = (trackingTransformLocal.position - panelRotationTransform.position).normalized;
            trackingLOS = CalculateTrackingLOS(normalized, ref blockingObject);

            if (interruptTracking)
            {
                Quaternion target = originalRotation * Quaternion.Euler(
                    0f,
                    TrackingAlignmentOffset + interruptAngle,
                    0f
                );

                panelRotationTransform.localRotation = Quaternion.Lerp(
                    panelRotationTransform.localRotation,
                    target,
                    TimeWarp.deltaTime * trackingSpeed
                );
            }
            else
            {
                base.CalculateTracking();
                return;
            }

            if (UIPartActionController.Instance != null &&
                UIPartActionController.Instance.ItemListContains(part, false))
            {
                if (trackingLOS)
                {
                    status = Localizer.Format("#Sunlock_TrackingInterrupted");
                }
                else
                {
                    CelestialBody bodyByName = FlightGlobals.GetBodyByName(blockingObject);
                    status = Localizer.Format("#autoLOC_234994",
                        (bodyByName != null) ? bodyByName.displayName : blockingObject);
                }
            }

            PostCalculateTracking(trackingLOS, normalized);
        }

        private bool ShouldShowInterrupt()
        {
            return deployState == DeployState.EXTENDED && isTracking;
        }
    }
}
