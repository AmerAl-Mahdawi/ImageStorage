# ImageStorage
--------------


Table of Contents:
------------------
* Introduction
* Configuring the application
* Running the application
* Use the Application
* Running Unit and Integration Tests
* Used Technologies
* External aws Services
* Application Projects


Introduction:
-------------
This is and RESTFull API application that has been created according to the specified requrements. It integrates with 3 aws servises in order to provide the service of uploading images of type jped or png with maximum size 500 KB (512000 B) into S3 bucket, and save images data into RDS server, and synced the information with aws ES service. In addition to search for images using the ES service with specific filters.


Configuring the application:
----------------------------
1. S3 creadentials should be saved in "%HOMEPATH%/.aws/credentials" file, example:

    [default]
    aws_access_key_id=AKIAIOSFODNN7EXAMPLE
    aws_secret_access_key=wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY

    And the region should be saved in "%HOMEPATH%/.aws/config" file, example:
    [default]
    region = ca-central-1 

    For more information, please check: "https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-files.html"

2. "appsettings.json": this file includes the DB connectionstring, and other aws services configurations.


Running the application:
------------------------
There are two modes in running this application:
1. Debug mode: This can be done by Visual Studio or any other IDE. I used VS 2019 for building this application. Open Visual Studio as administrator and open the solution of this application exists in the root folder of the linked zip file (you need to extract the files first from the zip file). This mode will allow you to see the code and to run the included unit and integration tests
2. Release mode: This can be done by navigating to "\ImageStorage\ImageStorage\bin\Release\netcoreapp3.1" locatin from the root folder of the application (after unzip the linked file) and run "ImageStorage" file wither by double clicking it or from the cmd. The application URL is https://localhost:5001/ (This is configurable).

Use the Application:
--------------------
You can use the tests in the Assets folder. The URL or these tests have been modified to call the application using port 5001


Running Unit and Integration Tests:
-----------------------------------
1. Open the solution in VS 2019
2. Open the Test Explorer sindow
3. Click: "Run All Tests In View" button


Used Technologies:
------------------
1. ASP.NET Core 3.1 using C# programming language
2. SQL for DB
3. xunit for unit and integration tests
4. SonarQube for checking the quality of the code
5. Moq library for mocking objects while running unit tests


External aws Services:
------------------
1. S3 service for saving the uploaded images physically
2. RDS DB server using MS SQL Server to save the images data
3. ES service for searching for images in the already apluaded by the application


Application Projects:
---------------------
 It consists of 6 projects organized in four folders as below:
1. Folder Api: This folder has two projects that considered the main part of the application:
a) ImageStorage: This project is responsible for handling the APIs call to the application. It has ImagesController.cs class that handles the Get APIs for searching for images using aws ES service depending on image descriptions. 
b) ImageStorage.Library: This is a library that handles all the communications and integrations with all the three used aws services by this application (RDS DB using MS SQL Server, S3 for uploading the images physically, and aws ES to search for images in the application using filters.

2. Folder Database: This folder has SQL Server Database Project that is used to publish the application's DB in any MS SQL server depending on the Connectionstring. This application is using aws RDS. This project has only one table names Images for holding the uploaded images' metadata. And it has one stored procedure that handles the insert instead of using simple query. This improved the security of the application.

3. Folder UnitTests: This folder has two projects for unit testing the application:
a) ImageStorage.Tests project for unit testing the ImageStorage project
b) ImageStorage.Library.Tests project for unit testing the ImageStorage.Library project

4. Folder IntegrationTests: This folder has only one project for integration testing the API calls to the application.

