@echo off
cd /d %~dp0
dotnet restore ParliamentOfficeEnterprise.sln
dotnet build ParliamentOfficeEnterprise.sln -c Release
pause
