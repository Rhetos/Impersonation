SETLOCAL

@REM Using "no-build" option as optimization, because Test.bat should always be executed after Build.bat.
dotnet test Impersonation.sln --no-build || GOTO Error0

@REM ================================================

@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
@EXIT /B 0

:Error0
@ECHO.
@ECHO %~nx0 FAILED.
@EXIT /B 1
