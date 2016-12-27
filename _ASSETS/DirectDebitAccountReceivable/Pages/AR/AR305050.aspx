<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="AR305050.aspx.cs" Inherits="Page_AR305050" Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" PrimaryView="Document" TypeName="PX.Objects.ACH.ARBatchEntry" PageLoadBehavior="GoLastRecord">
		<CallbackCommands>
			<px:PXDSCallbackCommand Name="Insert" PostData="Self" Visible="false" />
			<px:PXDSCallbackCommand CommitChanges="True" Name="Save" />
			<px:PXDSCallbackCommand Name="First" PostData="Self" StartNewGroup="true" />
			<px:PXDSCallbackCommand Name="Last" PostData="Self" />
			<px:PXDSCallbackCommand StartNewGroup="True" DependOnGrid="grid" Name="ViewARDocument" Visible="false" />
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:Content ID="cont2" ContentPlaceHolderID="phF" runat="Server">
	<px:PXFormView ID="form" runat="server" Style="z-index: 100" Width="100%" Caption="Batch Summary" DataMember="Document" DefaultControlID="edBatchNbr" FilesIndicator="True" NoteIndicator="True" LinkIndicator="True"
		NotifyIndicator="True" DataSourceID="ds" TabIndex="100">
		<Template>
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="S" />
			<px:PXSelector runat="server" DataField="BatchNbr" ID="edBatchNbr" />
			<px:PXDropDown runat="server" DataField="Status" ID="edStatus" Enabled="False" />
			<px:PXCheckBox CommitChanges="True" Size="" runat="server" DataField="Hold" ID="chkHold" />
			<px:PXDateTimeEdit CommitChanges="True" runat="server" DataField="TranDate" ID="edTranDate" />
			<px:PXTextEdit runat="server" DataField="ExtRefNbr" ID="edExtRefNbr" />
			<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="M" />
			<px:PXSegmentMask CommitChanges="True" runat="server" DataField="CashAccountID" ID="edCashAccountID" Enabled="False" />
			<px:PXSelector CommitChanges="True" runat="server" DataField="PaymentMethodID" ID="edPaymentMethodID" Enabled="False" />
			<px:PXSelector runat="server" DataField="ReferenceID" ID="edReferenceID" Enabled="False" />
            <px:PXDateTimeEdit  Size="M" runat="server" DataField="ExportTime" ID="edExportTime" Enabled="False" />
			<px:PXLayoutRule runat="server" ColumnSpan="2" />
			<px:PXTextEdit runat="server" DataField="TranDesc" ID="edTranDesc" />
			<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="S" />
			<px:PXNumberEdit runat="server" Enabled="False" DataField="CuryDetailTotal" ID="edCuryDetailTotal" />
            <px:PXTextEdit runat="server" DataField="BatchSeqNbr" ID="edBatchSeqNbr" />
			<px:PXNumberEdit runat="server" DataField="DateSeqNbr" ID="edDateSeqNbr" Enabled="False" />
		</Template>
	</px:PXFormView>
</asp:Content>
<asp:Content ID="cont3" ContentPlaceHolderID="phG" runat="Server">
	<px:PXGrid ID="grid" runat="server" Height="180px" AllowAutoHide="false" Width="100%" Caption="Payments" SkinID="Details">
		<%--<AutoCallBack Target="gridPaymentApplications" Command="Refresh" />--%>
		<Levels>
			<px:PXGridLevel DataMember="BatchPayments">
				<RowTemplate>
					<px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="SM" ControlSize="XM" />
					<px:PXDropDown ID="edARPayment__DocType" runat="server" DataField="ARPayment__DocType" />
					<px:PXSelector ID="edARPayment__RefNbr" runat="server" DataField="ARPayment__RefNbr" />
					<px:PXSegmentMask ID="edARPayment__CustomerID" runat="server" DataField="ARPayment__CustomerID" />
					<px:PXSegmentMask ID="edARPayment__CustomerLocationID" runat="server" DataField="ARPayment__CustomerLocationID" />
					<px:PXSelector ID="edARPayment__CuryID" runat="server" DataField="ARPayment__CuryID" />
					<px:PXTextEdit ID="edARPayment__DocDesc" runat="server" DataField="ARPayment__DocDesc" />
					<px:PXSelector ID="edARPayment__PaymentMethodID" runat="server" DataField="ARPayment__PaymentMethodID" />
					<px:PXTextEdit ID="edARPayment__ExtRefNbr" runat="server" DataField="ARPayment__ExtRefNbr" />
					<px:PXDateTimeEdit ID="edARPayment__DocDate" runat="server" DataField="ARPayment__DocDate" Enabled="False" />
					<px:PXNumberEdit ID="edARPayment__CuryOrigDocAmt" runat="server" DataField="ARPayment__CuryOrigDocAmt" /></RowTemplate>
				<Columns>
					<px:PXGridColumn DataField="ARPayment__DocType" Width="65px" Type="DropDownList" />
					<px:PXGridColumn DataField="ARPayment__RefNbr" Width="87px" LinkCommand="ViewARDocument" />
					<px:PXGridColumn DataField="ARPayment__CustomerID" Width="90px" />
					<px:PXGridColumn DataField="ARPayment__CustomerLocationID" Width="90px" />
					<px:PXGridColumn DataField="ARPayment__DocDate" Width="80px" />
					<px:PXGridColumn DataField="ARPayment__Status" />
					<px:PXGridColumn DataField="ARPayment__CuryID" Width="54px" />
					<px:PXGridColumn DataField="ARPayment__DocDesc" Width="150px" />
					<px:PXGridColumn DataField="ARPayment__PaymentMethodID" Width="81px" />
					<px:PXGridColumn DataField="ARPayment__ExtRefNbr" />
					<px:PXGridColumn DataField="ARPayment__CuryOrigDocAmt" TextAlign="Right" Width="81px" />
				</Columns>
			</px:PXGridLevel>
		</Levels>
		<ActionBar>
			<Actions>
				<AddNew Enabled="False" />
			</Actions>
			<CustomItems>
				<px:PXToolBarButton Text="View Payment">
				    <AutoCallBack Command="ViewARDocument" Target="ds" />
				</px:PXToolBarButton>
			</CustomItems>
		</ActionBar>
		<AutoSize Container="Window" Enabled="True" MinHeight="150" />
	</px:PXGrid>
</asp:Content>
