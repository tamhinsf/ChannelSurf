# Channel Surf for Microsoft Teams 

Moving to Microsoft Teams from Slack or starting fresh?  You've come to the right place.  Here's what Channel Surf can do for you:

* Re-create your existing Slack channel structure in Teams
* Archive Slack messages into HTML files and store them in Teams
* Copy Slack file attachments into Teams
* Bulk add new Teams channels

We'll add features as more Microsoft Teams APIs become available and as time permits.   

Need a solution to quickly clone, archive, un-archive your existing Microsoft Teams and manage the apps you've installed to them?  Check out [Quick Teams](https://github.com/tamhinsf/QuickTeams)

For now, let's get started!

## Identify your destination Team

You can use the Microsoft Teams app to create the Team you want to place your re-created or new channels within, or wait and create a new Team when you run Channel Surf.

Either way, you'll need to decide if you want to:

* Add entirely new channels 
	
	You'll edit a file called "channels.json" that contains the name and description of the channels you want to add.
* Re-create Slack channels
	
	You can create a Slack Team export on a self-service basis as a Slack Team Owner or Admin at this page [https://my.slack.com/services/export](https://my.slack.com/services/export).  Download the export file and tell Channel Surf its location.   We'll scan it and re-create the Slack channel structure in Teams - and give you the option to do more.  
	
	WARNING: Slack channels can be public or private - a concept not currently supported in Microsoft Teams.  This import tool will re-create all Slack channels in MS Teams, irrespective of whether they were public or private in Slack.  Want that to change?  Go ahead and [file an issue](https://github.com/tamhinsf/ChannelSurf/issues).

  * Archive Slack message history (optional)

    Create HTML files in Microsoft Teams that display the message history for each re-created Slack channel.  To view your HTML message archive, launch the Teams app, select a re-created Slack channel, open the Files tab, and browse to this path: channelsurf/messages/html.  You'll see one or more HTML files that contain your Slack channel messages.  
    
    Note: We'll improve the display of user names, date and time stamps, message formatting, and provide a better viewing experience in the future.  We're just getting started!

  * Archive Slack file attachments (optional)

    Copy file attachments associated with Slack messages into Microsoft Teams.  Of course, messages in your HTML message archive (described above) that contain file attachments will link to the copy of the file attachment we've copied to Microsoft Teams.   
    
    You an also access these archived file attachments directly by selecting the re-created channel in the Teams app, going to the Files tab, and navigating to the channelsurf/fileattachments folder.  You'll then see subfolders whose name maps to the file attachment's unique ID on Slack.  Open a subfolder, and you'll have access to the actual file attachment.

NOTE: Channel Surf uses features of the Microsoft Graph currently in preview (beta).  You may encounter unexpected errors and changes in behavior.  We'll do our best to keep up.

## Setup a development environment 

* Clone this GitHub repository.
* Install Visual Studio 2017.  Don't have it?  Download the free [Visual Studio Community Edition](https://www.visualstudio.com/en-us/products/visual-studio-community-vs.aspx)
* Don't want to use Visual Studio?  Channel Surf was written using .NET Core 1.1 and runs on Windows, macOS, and Linux.  Instead of using Visual Studio, you can simply download the SDK necessary to build and run this application.
  * https://www.microsoft.com/net/download/core

## Identify a test user account

* Sign in to your Office 365 environment as an administrator at [https://portal.office.com/admin/default.aspx](https://portal.office.com/admin/default.aspx)
* Ensure you have enabled Microsoft Teams for your organization [https://portal.office.com/adminportal/home#/Settings/ServicesAndAddIns](https://portal.office.com/adminportal/home#/Settings/ServicesAndAddIns)  
* Identify a user whose account you'd like to use 
  * Alternatively, you can choose to use your Office 365 administrator account 

## Create the Channel Surf Application in Azure Active Directory

You must register this application in the Azure Active Directory tenant associated with your Office 365 organization.  

* Sign in to your Azure Management Portal at https://portal.azure.com
    * Or, from the Office 365 Admin center select "Azure AD"
* Within the Azure Portal, select Azure Active Directory -> App registrations -> New application registration  
    * Name: ChannelSurfCli (anything will work - we suggest you keep this value)
    * Application type: Native
    * Redirect URI: https://channelsurf-cli (anything else will work if you want to change it)
     * NOTE: In earlier versions of this code, we hard-coded this value in Program.cs.  It's now been moved to appsettings.json and we've agained defaulted it to https://channelsurf-cli
    * Click Create
* Once Azure has created your app, copy your Application Id and give your application access to the required Microsoft Graph API permissions.  
   * Click your app's name (i.e. ChannelSurfCli) from the list of applications
   * Copy the Application Id
   * All settings -> Required permissions
     * Click Add  
     * Select an API -> Microsoft Graph -> Select (button)
     * Select permissions 
	   * Read all users' full profiles
	   * Read and write all groups
     * Click Select
     * Click Done
	
  * If you plan to run Channel Surf as a non-administrator: applications built using the Graph API permissions above require administrative consent before non-administrative users can sign in - which fortunately, you'll only need to do once.  
    * You can immediately provide consent to all users in your organization using the Azure Portal. Click the "Grant permissions" button, which you can reach via your app's "Required permissions" link.
      * Here's the full path to "Grant permissions": Azure Active Directory -> App registrations -> Your app (i.e. ChannelSurfCli) -> All settings ->  Required permissions -> Grant permissions
    * Or, whenever you successfully launch ChannelSurfCli, we'll show you a URL that an administrative user can visit to provide consent.
      * Note: if you've configured the re-direct URL to be the same value as we've shown you on this page (i.e. https://channelsurf-cli), you'll be sent to an invalid page after successfully signing in.  Don't worry!
* Take note of your tenant name, which is typically in the form of your-domain.onmicrosoft.com.  You'll need to supply this when building or running ChannelSurfCli.


## Define the Microsoft Teams channels you wish to create

At this point, you need to decide if you'll create new channels or re-create Slack Teams channels.  Go to the section for the operation you wish to perform.

* Add entirely new channels 
* Re-create Slack channels

### Add entirely new channels 
* Open the cloned code from this repository, and navigate to the folder "ChannelSurfCli"
* Open the file named channels.json
  * This file's name must remain channels.json.  Backup the original if you want.  We'll provide more flexibility in a future release.
  * Take note of the fields used to define channel names and descriptions.  We've hopefully made it easy to figure out!
  * You can repeat the JSON structure to the number of teams you want to create.
  * This file mimics the file format used to store channel definitions from a Slack Team export file (next section)

### Re-create Slack channels
* Go to this page https://my.slack.com/services/export
* Sign in to your Slack Team as an Owner or Administrator
* Click Start Export 
* You'll receive a notification when your export is ready for download
* Download the export.  It's a ZIP file and will be named in this format: "Your Team export Month Day Year.zip"
* We recommend you re-name the export ZIP file to not have spaces. This will make it easier to work with: i.e. myexport.zip.

IMPORTANT: You Slack export contains a security token that can be used by ANYONE to download files you've attached to messages.  Handle your export file with extreme security.  Go back to the [Slack Export page](https://my.slack.com/services/export) when you have downloaded your archive and revoke "Your Team’s Export File Download Tokens".  
  
## Build ChannelSurfCli

* Open the cloned code from this repository in Visual Studio, Visual Studio Code, or your favorite editor
 * Update appsettings.json in the ChannelSurfCli folder.  Within the section called "AzureAd", update the fields with your tenant name (TenantId), Application ID (ClientId), and Redirect URI (AadRedirectUri).  You can leave the value for AadInstance as-is.
  * NOTE: In previous versions of ChannelSurfCli, these values were stored in Program.cs.  If you're upgrading to this new version, please move your settings into appsettings.json as described above.  
 * Or, you can leave these values empty and provide them whenever you run the application.
* Build ChannelSurfCli
  * In Visual Studio select ChannelSurfCli from the Solution Explorer, then from the top menu pick Build -> BuildSurfCli
  * Or, using the .NET Core SDK, you can perform these steps from the command line
    * Open a command prompt, and navigate to the ChannelSurfCli folder 
      * dotnet restore
      * dotnet build

## Using ChannelSurfCli
 
* Launch the Microsoft Teams app and make sure you are a member of an existing Team.  If not - create a Team!
* Open a command prompt, and navigate to the ChannelSurfCli folder
* Run one of the two following commands, based on whether you're creating channels from a JSON file or a Slack Teams export
   * dotnet run /path/to/channels.json
   * dotnet run /path/to/slack/archive/myexport.zip (replace myexport.zip with your archive filename)
* If prompted, provide your Active Directory Tenant Name and Application Id
* Follow the instructions provided to sign in:
   * Start a web browser and go to https://aka.ms/devicelogin - we strongly suggest you use your web browser's "private mode".
   * Enter the security code provided in the command prompt
   * Consent to using ChannelSurfCli
   * Enter your O365 credentials.  
   * Return to your command prompt 
* Select the target Microsoft Team you want to create or re-create your channels into.  You can also choose to create a new Microsoft Team.
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

Copyright (c) 2018 Tam Huynh. All rights reserved. 


### Disclaimer ###
**THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.**
