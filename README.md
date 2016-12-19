# A self-contained Azure Media Service (AMS) End-to-End Szenario with Dynamic AES 128 Encryption

This repository contains a end-to-end solution to show how dynamic AES 126 encryption works within a single Visual Studio Solution. 

**You will find 3 projects in the project:**

1. **ADFS-Mockup**

   This is a very easy ADFS mockup using [IdentityServer4](http://docs.identityserver.io/en/release/). I used IdentityServer to simply show how authentication works, such that you don't have to set up your own ADFS to just try out this demo. 
 

2. **CMS**
 
   This is a very simple CMS Mockup. User can sign in to the CMS as one of two pre-configured users and see different videos regarding the position of the user in the company. Here you can also see how a man-in-the-middle attack is  prohibited. 
   If the client opens a video URL of a video without the rights to view it, you will get an error. 

3. **AMS-Setup**

   This project is to setup your Azure Media Services Account: upload your videos as assets, encode them and specify the encryption details for the assets.  

## Setup

1. **Download two videos:** Navigate to the folder **Solution Items** and just run **"Download_Videos.cmd"**. This will download two different Azure Media Services videos that will be uploaded to your AMS account. 
   
2. In the folder **Solution Items**, set your own Azure Media Services account details in the **"Setup_Environment.cmd"** and run it. 

3. Specify **AMSsetup** as startup project and run it. 

4. Specify **CMS** and **ADFS-Mockup** as Startup Projects and run the solution. 

5. Take a look at how authentication goes :) 


## What can I do?

   Imagine a company. The **CMS** is an intranet application to show training videos to my employees. 
   There are two types of employees in my company: "normal" employees and employees who work in the management. The training content for the management is different, focusing on videos about compliance, company goals and reporting. 
   The "normal" staff should not be able to see the videos dedicated to the management. 

   **To realize this, I created two clients in IdentityServer:**

   1. A "normal" employee, called **staff**. This employee can view all videos in the scope "Staff". 
    * Username: StaffName
    * Password: pdw1
   2.  An employee working in the **management**. This employee can only view videos in the scope "Management".  
    * Username: ManagementName
    * Password: pwd2

**You should recognize:**

 * You should see different videos when you perform the Login to the CMS with the two different users. 
 * If you copy the video URL while logged in as a Management employee and try to view it as Staff employee, you will get an error. 
    

## Explanation
What is happening inside?

**1. AMSSetup**

   * Upload your video files to AMS as assets or use existing assets via their id. 
   * Encode your assets. 
   * Setup the AES Encryption. [Here](https://docs.microsoft.com/en-us/azure/media-services/media-services-protect-with-aes128) you can find a detailed explanation.
 

**2. Client performs Log-in to the CMS:**

  * Client wants to access the CMS. 
  * CMS redirects the Client to ADFS-Mockup (IdentitiyServer). 
  * ADFS-Mockup verfies clientname and client credentials. If successful, IdentityServer sends a bearer token back to the client.
  * Client uses Bearer Token to get access to the CMS. 
  * Client can see the video page. 

**3. Decryption of the videos: Videos must be decrypted before the client can see them.**

  * Determine the role (staff|management) of the client via the session. 
  * Query the video database to get all videos for the client's role (staff|management). 
  * Create JWT Token. It is used to get the decryption key of the video.
  * Client can see videos regarding his role (staff|management). 

![Overview](https://github.com/juliajauss/AMSAes128EndToEnd/blob/master/overview.png)

