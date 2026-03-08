### This is the backend that fulfills the [ProjectRequirementsDoc](../ProjectRequirementsDoc.md)

### Basic Info
This will be hosted in MS Azure.
It will run on a very cheap function app that runs once every 3 hours, and will write data to a json file YYYY-MM-dd-HH.json in blob storage.

### Code
Use C# dotnet 10

### CI/CD
- Create a local build pipeline with scripts that I can just invoke from the command prompt.
- Create AZURE IaC for the function app and it's settings.
- document how to run these. 

