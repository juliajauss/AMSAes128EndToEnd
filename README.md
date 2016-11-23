# A self-contained Azure Media Service (AMS) End-to-End Szenario with Dynamic AES 128 Encryption

This repository contains a end-to-end solution to show how Dynamic AES 126 encryption works within a single Visual Studio Solution. 

**You will find 3 projects in the project:**

1. **ADFS-Mockup**

   This is a very easy ADFS mockup using [IdentityServer4](http://docs.identityserver.io/en/release/). I used IdentityServer to simply show how authentication works, such that you don't have to set up your own ADFS to just try out this demo. 
 

2. **CMS**
 
   This is a very simple CMS Mockup. User can sign in to the CMS as one of two pre-configured users and see different videos regarding the position of the user in the company. Here you can also see how a man-in-the-middle attack is  prohibited. 
   If the client opens a video URL of a video without the rights to view it, you will get an error. 

3. **AMS-Setup**

   This project is to setup your Azure Media Services Account: upload your videos as assets, encode them and specify the encryption details for the assets.  

## Setup
**You need to customize these things to run this demo:**

1. Navigate to the **AMS-Setup** project, open the **"App.config"** file and insert your Azure Media Service credentials. Run AMS Setup. 
```xml
   <appSettings>
    <add key="MediaServicesAccountName" value="<name-of-your-AzureMediaServices-account, e.g. juliasmediaservice" />
    <add key="MediaServicesAccountKey" value="<your-AzureMediaServices-AccountKey" />
    <add key="StaffVideoFile" value ="<path-to-the-video-or-asset-id"/>
    <add key="ManagementVideoFile" value ="<path-to-the-video-or-asset-id"/>
    <add key="Issuer" value="http://localhost:5000/identity" />  //This is the URL of your Identity Provider, in this case the IdentitiyServer 
    <add key="Staff" value="Staff" /> 
    <add key="Management" value="Management" />
    <add key ="PrimaryVerificationKey" value="yLUn9dp8IGeosxy14Com0nUt5Wvi/YLV48agTlPoWAFMcH2dvAh307UX7Pi0tS5W4vS85OcqTAfuVvVFjNfybg=="/>
    <add key="ClientSettingsProvider.ServiceUri" value="" />
  </appSettings> 
```

   
2. Specify CMS and ADFS-Mockup as Startup Projects and click Start. 

3. Take a look at how authentication goes :) 


## What can I do?
   Imagine a company "JJ & Rob Adventures". There you can book several adventures for your holidays, like diving, paragliding, sailing, ..and so on. 
   My CMS is an Intranet Application to show training videos to my employees. 

   There are two types of employees in my company: "normal" staff - these are the instructors for the activities. Videos contain infos about Do's & Dont's e.g. don't go paragliding if it is stormy.
   Management employees - managing the instructors and the company. Video contain infos about compliance, company goals and necessary reporting. 
   Of course the "normal" staff should not be able to see the videos dedicated to the management. 

   **To realize this, I created two clients in IdentityServer:**

   1. A "normal" employee, called **staff**. This employee can view all videos in the scope "Staff". 
   * Username: StaffName
   * Password: pdw1
   2.  A employee working in the **management**. This employee can only view videos in the scope "Management". Of course you can configure the solution to show all videos to a employee in the management. 
   * Username: ManagementName
   * Password: pwd2

**You should recognize:**

 * You should see different Videos when you perform the Login to the CMS with the two different users. 
 * If you copy the video URL while logged in as a Management employee and try to view it as Staff employee, you will get an error. 
    

## Explanation
What is happening inside?

1. AMSSetup
   * Upload your video files to AMS as assets or use existing assets via their id. 
   * Encode your assets. 
   * Setup the AES Encryption. [Here](https://docs.microsoft.com/en-us/azure/media-services/media-services-protect-with-aes128) you can find a detailed explanation.
 

1. Client performs Log-in to the CMS:
  * Client wants to access the CMS. 
  * CMS redirects the Client to ADFS-Mockup (IdentitiyServer). 
  * ADFS-Mockup verfies clientname and client credentials and if successful, sends a bearer token back to the client.
  * Client uses Bearer Token to get access to the CMS. 
  * Client can see the video page. 


2. Decrypt the videos: Videos must be decrypted before the client can see them. 
  * Determine the role (staff|management) of the client via his access token. 
  * Query the video database to get all videos for the client's role (staff|management). 
  * Foreach video: 
    1. Fetch manifest 
    2. Create JWT Token
  * Client can see videos regarding his role (staff|management). 




