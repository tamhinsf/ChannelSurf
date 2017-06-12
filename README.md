# Channel Surf for Microsoft Teams 

Quickly create Microsoft Teams channels from scratch or from your existing Slack Team.

  * This application uses features of the Microsoft Graph currently in preview (beta).  You may encounter unexpected errors and changes in behavior.  We'll do our best to keep up.

## Get started quickly with Microsoft Teams 

We've made it fast and easy to create channels in Microsoft Teams.  Use the Microsoft Teams app to create a Team you want to place your channels within.  Then, you can:

* Define new channels to create within a file called "channels.json"
* Re-create all the channels from your existing Slack Team.  How?  You can create a Slack Team export on a self-service basis as a Slack Team Owner or Admin.  Then, provide this Slack Export export to our app.  It's that easy!

## Setup a development environment 

* Clone this GitHub repository.
* Install Visual Studio 2017.  Don't have it?  Download the free [Visual Studio Community Edition](https://www.visualstudio.com/en-us/products/visual-studio-community-vs.aspx)
* Don't want to use Visual Studio?  Channel Surf was written using .NET Core 1.1 and runs on Windows, macOS, and Linux.  Instead of using Visual Studio, you can simply download the SDK necessary to build and run this application.
  * https://www.microsoft.com/net/download/core

## Identify a test user account

* Sign in to your Office 365 environment as an administrator at [https://portal.office.com/admin/default.aspx](https://portal.office.com/admin/default.aspx)
* Ensure you have enabled Microsoft Teams for your organization [https://portal.office.com/adminportal/home#/Settings/ServicesAndAddIns](https://portal.office.com/adminportal/home#/Settings/ServicesAndAddIns)  
* Find a user whose account you'd like to use with this example
  * Alternatively, you can choose to use your Office 365 administrator account 

## Create the Channel Surf CLI Application in Azure Active Directory

You must register this application in the Azure Active Directory tenant associated with your Office 365 organization.  

* Sign in to your Azure Management Portal at https://portal.azure.com
    * Or, from the Office 365 Admin center select "Azure AD"
* Select Active Directory -> App registrations -> New application registration  
    * Name: ChannelSurfCli (anything will work - we suggest you keep this value)
    * Application type: NATIVE CLIENT APPLICATION
    * Redirect URI: https://channelsurf-cli (anything will work - however, this is the value we have hard-coded into our application code in Program.cs)
* Once Azure has created your app, copy your Application Id and give your application access to the required Microsoft Graph API permissions.  
   * Click your app's name (i.e. ChannelSurfCli) from the list of applications
   * Copy the Application Id
   * All settings -> Required permissions
     * Click Add  
     * Select an API -> Microsoft Graph -> Select (button)
     * Select permissions 
	   * Read all users' full profiles
	   * Read and write all groups
	 * Done
* Applications built using the Graph API permissions above require administrative consent before non-administrative users can sign in - which fortunately, you'll only need to do once. If you have trouble signing in:
  * You can immediately provide consent to all users in the Azure Portal by selecting:
    * Azure Active Directory -> App registrations -> Your app (i.e. ChannelSurfCli) -> All settings ->  Required permissions -> Grant permissions
  * Or, whenever you successfully launch ChannelSurfCli, we'll show you the URL you can visit to provide admin consent.  Sign in as the admin for the O365 tenant you've configured Channel Surf CLI to work with. 
   * Note: if you've configured the re-direct URL to be the same value as we've shown you on this page (i.e. https://channelsurf-cli), you'll be sent to an invalid page after successfully signing in.  Don't worry!
* Take note of your tenant name, which is typically in the form of your-domain.onmicrosoft.com.  You'll need to supply this when building or running ChannelSurfCli.


## Define the Microsoft Teams channels you wish to create

At this point, you need to decide if you'll create new channels or re-create Slack Teams channels.  Go to the section for the operation you wish to perform.

* Define channels in channels.json
* Create your Slack Team export file

### Define channels in channels.json
* Open the cloned code from this repository, and navigate to the folder "ChannelSurfCli"
* Open the file named channels.json
  * This file's name must remain channels.json.  Backup the original if you want.  We'll provide more flexibility in a future release.
  * Take note of the fields used to define channel names and descriptions.  We've hopefully made it easy to figure out!
  * You can repeat the JSON structure to the number of teams you want to create.
  * This file mimics the file format used to store channel definitions from a Slack Team export file (next section)

### Create your Slack Team export file
* Go to this page https://my.slack.com/services/export
* Sign in to your Slack Team as an Owner or Administrator
* Click Start Export 
* You'll receive a notification when your export is ready for download
* Download the export.  It's a ZIP file and will be named in this format: "Your Team export Month Day Year.zip"
* We recommend you re-name the export ZIP file to not have spaces. This will make it easier to work with: i.e. myexport.zip.  
  
## Build ChannelSurfCli

* Open the cloned code from this repository in Visual Studio, Visual Studio Code, or your favorite editor
 * Update Program.cs in the ChannelSurfCli folder with your tenant name (aadTenant) and Application ID (aadAppClientId) 
  * Or, you can leave these values empty and provide them when you run the application.
* Build the app
 * In Visual Studio select ChannelSurfCli from the Solution Explorer, then from the top menu pick Build -> BuildSurfCli
  * Or, using the .NET Core SDK, you can perform these steps from the command line
    * Open a command prompt, navigate to the ChannelSurfCli folder 
	* dotnet restore
	* dotnet build

## Using ChannelSurfCli
 
* Launch the Microsot Teams app and make sure you are a member of an existing Team.  If not - create a Team!
* Open a command prompt, navigate to the ChannelSurfCli folder
* Run one of the two following commands, based on whether you're creating channels from a JSON file or a Slack Teams export
 * dotnet run /path/to/channels.json
 * dotnet run /path/to/slack/archive/myexport.zip (i.e. whatever you may have re-named your Slack Team export)
* Provide your Active Directory Tenant Name and Application ID if prompted
* Follow the instructions provided to sign in: start a web browser, enter the security code provided, and then your O365 credentials.  We strongly suggest you use your web browser's "private mode".
* After you login through the web browser return to your command prompt 
* Select the target Microsoft Team you want to create or re-create your channels into.
* Go back to the Microsoft Team app and explore the channels you added!

## Questions and comments

We'd love to get your feedback about this sample. You can send your questions and suggestions to us in the Issues section of this repository.

Questions about Microsoft Graph development in general should be posted to [Stack Overflow](http://stackoverflow.com/questions/tagged/microsoftgraph). Make sure that your questions or comments are tagged with [microsoftgraph].

## Additional resources

* [Use the Microsoft Graph API to work with Microsoft Teams](https://developer.microsoft.com/en-us/graph/docs/api-reference/beta/resources/teams_api_overview)
* [Microsoft Graph Beta Endpoint Reference](https://developer.microsoft.com/en-us/graph/docs/api-reference/beta/beta-overview)
* [Microsoft Graph API Explorer](https://developer.microsoft.com/en-us/graph/graph-explorer)
* [Overview - Microsoft Graph](https://developer.microsoft.com/en-us/graph/docs)
* [Microsoft Teams - Dev Center](https://dev.office.com/microsoft-teams)

## Copyright

Copyright (c) 2017 Tam Huynh. All rights reserved. 


### Disclaimer ###
**THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.**