# A self-contained Azure Media Service (AMS) End-to-End Szenario with Dynamic AES 128 Encryption

<a href="https://azuredeploy.net/?repository=https://github.com/juliajauss/resizingService" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>

This repository contains a end-to.end solution to show how Dynamic AES 126 encryption works within a single Visual Studio Solution. 

You will find 3 projects in the project:

1. **ADFS-Mockup**

   This is a very easy ADFS mockup using IdentityServer4 (Link: xx). I used IdentityServer to simply show how authentication works, such that you don't have to set up your own ADFS to just try out this demo. 
 

2. **CMS**
 
   This is a very simple CMS Mockup. User can sign in to the CMS and can than see the videos regarding their position in the company. Here you can also see how a Man-in-the-middle attack is  prohibited. 
   If the client opens a video URL of a video without the rights to view it, you will get an error. 

3. **AMS-Setup**

   This project is to setup your Azure Media Services Account: upload your videos as assets, encode them and specify the encryption for the assets.  

## Setup
**You need to customize these things to run this demo:**

1. Navigate to the **AMS-Setup** project, open the **"App.config"** file and insert your Azure Media Service credentials. Run AMS Setup. 

> [TODO: Insert picture] 

   
2. Specify CMS and ADFS-Mockup as Startup Projects and click Start. 

3. Take a look at how authentication goes :) 


## What can I do?
   Imagine my company "JJ & Rob Adventures". There you can book several adventures for your holidays, like diving, paragliding, sailing, ..and so on. 
   My CMS is an Intranet Application to show training videos to my employees. 
   There are two types of employees in my company: 
    - "Normal" staff - these are the instructors for the activities. Videos contain infos about Do's & Dont's e.g. don't go paragliding if it is stormy.
    - Management employees - managing the instructors and the company. Video contain infos about compliance, company goals and necessary reporting. 

   Of course the "normal" staff should not be able to see the dedicated video for the management. 

   Therefore, I created two clients in IdentityServer:
    a) A "normal" employee in the scope "Staff". This employee can view all videos in the scope "Staff". 
       Username: StaffName
       Password: pdw1
   
    a) A employee working in the management. This employee can view all videos in the scope "Staff" and additional all videos in the scope "Management". 
        Username: ManagementName
        Password: pwd2

    **You should recognize:**
    1. You should see different Videos when you perform the Login to the CMS with the two different users. 
    2. If you copy the video URL while logged in as a Management employee and try to view it as Staff employee, you will get an error. 
    

## Explanation
How does it work and what can I see? 

1. Setup AMS
 
   a) Specify the file path of video files you want to upload. Or if you already uploaded your video files to AMS as assets, get the assets via their id. 
   
   b) Encode your assets. 

   c) Setup the AES Encryption:
 

1. Authenticate the client to Log-in to the CMS 
   a) Client wants to accees the CMS. 
   b) CMS redirects the Client to our ADFS-Mockup (IdentitiyServer). 
   c) If ADFS-Mockup verfies the Clientname and Client Credentials, it sends the client a Bearer Token back.
   d) Client uses Bearer Token to get access to the CMS. 
   e) Client can see videos regarding his role (staff|management). 

2. Decrypt the videos
   a) 
- 


  - attach your image to the POST request
- Response
  - **[string, string]** - a dictionary of URLs in the JSON format which contain all the generated resized versions of your uploaded image with the size as the key

> Response example
~~~~
{
 "original":"https://thumbfuncstorage.blob.core.windows.net/originals/mario/mario.jpeg",
 "200":"https://thumbfuncstorage.blob.core.windows.net/sized/200/mario/mario.jpeg",
 "100":"https://thumbfuncstorage.blob.core.windows.net/sized/100/mario/mario.jpeg",
 "80":"https://thumbfuncstorage.blob.core.windows.net/sized/80/mario/mario.jpeg",
 "64":"https://thumbfuncstorage.blob.core.windows.net/sized/64/mario/mario.jpeg",
 "48":"https://thumbfuncstorage.blob.core.windows.net/sized/48/mario/mario.jpeg",
 "24":"https://thumbfuncstorage.blob.core.windows.net/sized/24/mario/mario.jpeg"
 }
~~~~




