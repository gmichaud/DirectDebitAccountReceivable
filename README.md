[![Project Status](http://opensource.box.com/badges/active.svg)](http://opensource.box.com/badges)

Direct Deposit in Account Receivable
==================================

An extension that allows to create batch payments and generate ACH Files to use Direct Deposit as a payment method in Account Receivable.

Quick Start
-----------

### Installation

##### Install the customization project
1. Download DirectDepositAccountReceivable.zip from this repository
2. In your Acumatica ERP instance, import DirectDepositAccountReceivable.zip as a customization project
3. Publish the customization project

### Configuration
These instructions assume that Fedwire has been setup for AP and it is already in use.
1. Financials – Cash Management – Setup – Payment Methods. Choose FEDWIRE or the name you called your ACH method. Then go to the tab labeled Settings for use in AR. Select the export scenario as it shows on the image below and then click auto configure.
![Screenshot](/_ASSETS/ReadMe/1.png)

2. Go to the work area and select the cash account you intend to use for the ACH in AR.
3. Make sure the payment method you just edited in 1. Is listed in this cash account payment method tab. Also enter the AR last reference number if you didn’t set it up on the setup area.
4. While in the cash account go to the remittance tab and enter the bank information for your cash account under the payment method you intend to use with ACH
![Screenshot](/_ASSETS/ReadMe/2.png)


### Setting up the Customer for Direct Deposit
Next we need to setup the customer and the account you will be using to pull the funds from.
Financials – Account Receivable – Work area – Customers Payment Methods
Add the ACH or FEDWIRE method to the customer. As well as the customer bank acct and routing number as prompted on this screen.
![Screenshot](/_ASSETS/ReadMe/3.png)


### Processing Direct Deposit Payments
1. Under Finance/Accounts Receivable/Process tab, go to Direct Deposit Processing - prepare payments. Then check the payments you would like to process and hit the process button on top. If you want to process all payments listed then Press the Process All button on top.
![Screenshot](/_ASSETS/ReadMe/4.png)

2. Go to the next menu item: Process Payments. Enter your payment method and cash account. You should see listed the payments you chose in the prior step or any payments that have been prepared previously. Once again Mark the payments you would like to process and press the button on top that sais Process or Process all depending on what you would like to do.
![Screenshot](/_ASSETS/ReadMe/5.png)

3. Release batch and export file. Just like in Payables the system will take you to the Batch Payment screen in AR. Here take your batch out of hold and release the batch. Once it has been released then press the export button to create your ACH file.
![Screenshot](/_ASSETS/ReadMe/6.png)

4. Download file so you can import to your bank website. You will notice a 1 by Files on top. That means there is one file ready for you to download and upload to your bank website. To download the file, press the word FILES(1) on the top right. A smaller screen will pop up. Showing you the ACH file that was created. Press the down arrow to download the file. The file is now sitting in your download folder to be uploaded to the bank.
![Screenshot](/_ASSETS/ReadMe/7.png)


Known Issues
------------
None at the moment

## Copyright and License

Copyright © `2016` `Acumatica`

This component is licensed under the MIT License, a copy of which is available online [here](LICENSE)
