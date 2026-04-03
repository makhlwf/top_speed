using System;

namespace TopSpeed.Vehicles.Control
{
    internal sealed class DefaultControlArbiter : IControlArbiter
    {
        public CarControlIntent ResolveIntent(
            ICarController primaryController,
            ICarController? overrideController,
            in CarControlContext context)
        {
            if (overrideController != null)
                return overrideController.ReadIntent(context);
            if (primaryController == null)
                throw new InvalidOperationException("Primary car controller is not configured.");

            return primaryController.ReadIntent(context);
        }
    }
}

