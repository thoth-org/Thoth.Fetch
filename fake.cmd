SET TOOL_PATH=".fake"

IF NOT EXIST "%TOOL_PATH%\fake.exe" (
  rem #if (version == "latest")
  dotnet tool install fake-cli --tool-path ./%TOOL_PATH%
  rem #else
  dotnet tool install fake-cli --tool-path ./%TOOL_PATH% --version 5.16.1
  rem #endif
)

"%TOOL_PATH%/fake.exe" %*
