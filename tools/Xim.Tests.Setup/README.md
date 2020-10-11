# Xim.Tests.Setup

_Simple .Net Core 3.1 tool to install SSL development certificate required for Http Api and Service Bus secure communication._

## Why

The purpose of this tool is to register a SSL certificate to allow localhost development with Xim simulators. As an example, [Microsoft.Azure.ServiceBus](https://www.nuget.org/packages/Microsoft.Azure.ServiceBus) works only over secure AMQP connection and thus requires such certificate.

## How

Open `Xim.Tests.Setup.csproj` in Visual Studio and run the project. This will generate ad install self-signed SSL localhost development certificate into the required certificate stores.

**Admin permissions are required** to run the tool properly.

## Manual Certificate Install

You can also install the `Xim.Development.pfx` file manualy using Manage Computer Certificates into 1. `Personal > Certificates` and 2. `Trusted Root Certification Authorities > Certificates`.

The password for the .pfx file is: `Xim`

You will need to set access to all local Users for the private key in Manage Computer Certificates:
1. Select `Personal > Certificates > Xim Test Certificate`,
2. Right click the certificate and select `All Tasks > Manage Private Keys`,
3. Click `Add` select your local PC and enter Users,
4. Set Users to full access for the private key.