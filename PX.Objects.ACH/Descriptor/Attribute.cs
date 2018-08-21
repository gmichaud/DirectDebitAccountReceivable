using PX.Data;
using PX.Objects.CA;
using System;

namespace PX.Objects.ACH
{
    public class DecryptACHValuesAttribute : PXEventSubscriberAttribute, IPXRowSelectedSubscriber
    {
        private Type _PaymentMethodID;

        public DecryptACHValuesAttribute(Type paymentMethodID)
        {
            _PaymentMethodID = paymentMethodID;
        }

        public void RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            if (e.Row == null) return;

            var paymentMethodID = sender.GetValue(e.Row, _PaymentMethodID.Name) as string;

            if (!String.IsNullOrEmpty(paymentMethodID))
            {
                var paymentMethod = PXSelect<PaymentMethod,
                                                Where<PaymentMethod.paymentMethodID,
                                                    Equal<Required<PaymentMethod.paymentMethodID>>>>
                                            .Select(sender.Graph, paymentMethodID);
                var paymentMethodExt = PXCache<PaymentMethod>.GetExtension<PaymentMethodExt>(paymentMethod);
                if (paymentMethodExt != null && paymentMethodExt.ARCreateBatchPayment == true)
                {
                    PXRSACryptStringAttribute.SetDecrypted(sender, base.FieldName, true);
                }
            }
        }
    }
}
