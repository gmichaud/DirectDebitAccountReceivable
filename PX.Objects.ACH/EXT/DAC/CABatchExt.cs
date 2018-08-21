﻿using PX.Data;
using PX.Objects.CA;

namespace PX.Objects.ACH
{
    [PXTable(typeof(CABatch.batchNbr), IsOptional = true)]
    public class CABatchExt : PXCacheExtension<CABatch>
    {
        #region BatchModule
        public abstract class batchModule : PX.Data.IBqlField
        {
        }

        [PXDBString(2, IsFixed = true)]
        [PXDefault(GL.BatchModule.AP)]
        [PXStringList(new string[] { GL.BatchModule.AP, GL.BatchModule.AR }, new string[] { "AP", "AR" })]
        [PXUIField(DisplayName = "Module", Enabled = false)]
        public string BatchModule { get; set; }
        #endregion
    }
}
