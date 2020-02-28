using System;

namespace ManualMF
{
    internal enum AccessState : byte { Pending = 0, Allowed = 1, Denied = 2 };
    internal enum AccessDeniedReason : byte { UnknownOrNotDenied=0, DeniedByOperator, DeniedByIP, DeniedByTimeOut, RecordDisappeared }
    internal struct AccessStateAndReason
    {
        public AccessState State;
        public AccessDeniedReason Reason;
        public AccessStateAndReason(AccessState _State, AccessDeniedReason _Reason) { State = _State; Reason = _Reason; }
        /* Commented out because default ValueType imolemetation seems to be suitable
        public override bool Equals(object obj)
        {
            if (null == obj) return false;
            if (typeof(AccessStateAndReason) == obj.GetType())
            {
                return this.State == ((AccessStateAndReason)obj).State && this.Reason == ((AccessStateAndReason)obj).Reason;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return State.GetHashCode() ^ Reason.GetHashCode();
        }
        */
    }

}
