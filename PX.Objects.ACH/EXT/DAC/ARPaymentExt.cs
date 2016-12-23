using PX.Data;
using PX.Objects.AR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.ACH
{
    [PXTable(typeof(ARPayment.docType), typeof(ARPayment.refNbr), IsOptional = true)]
    public class ARPaymentExt : PXCacheExtension<ARPayment>
    {
        //Note: Might need to move it in ARRegister
        #region Passed
        public virtual bool? Passed { get; set; }
        #endregion

        #region StubCntr
        public abstract class stubCntr : PX.Data.IBqlField
        {
        }

        /// <summary>
        /// The counter of the related pay stubs.
        /// Note that this field is used internally for numbering purposes and its value may not reflect the actual count of the pay stubs.
        /// </summary>
		[PXDBInt()]
        [PXDefault(0)]
        public virtual Int32? StubCntr { get; set; }
        #endregion
        #region BillCntr
        public abstract class billCntr : PX.Data.IBqlField
        {
        }

        /// <summary>
        /// The counter of the related bills.
        /// Note that this field is used internally for numbering purposes and its value may not reflect the actual count of the bills.
        /// </summary>
		[PXDBInt()]
        [PXDefault(0)]
        public virtual Int32? BillCntr { get; set; }
        #endregion

        #region Printed
        public abstract class printed : PX.Data.IBqlField
        {
        }

        /// <summary>
        /// When set to <c>true</c> indicates that the document was printed.
        /// </summary>
		[PXDBBool()]
        [PXDefault(false)]
        public Boolean? Printed { get; set; }
        #endregion
    }
}
