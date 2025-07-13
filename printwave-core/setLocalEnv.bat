@echo off
REM Load .env file into environment variables

for /f "usebackq tokens=1,* delims==" %%A in (`findstr /V "^#" .env`) do (
    if not "%%A"=="" (
        set "%%A=%%B"
    )
)

echo Environment variables loaded from .env
mvnw spring-boot:run