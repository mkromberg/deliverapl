# Start "dyalog/jarvis" docker image to server "involute" web service
# See invoke.ps1 for an example call to the running service
# 
$vol = "C:\Devt\deliverapl\WebService:/app" # Map application folder to "/app"
$ride = "RIDE_INIT=SERVE:*:4502"            # Allows RIDE connections for debugging (remove for prod deployment)
# -p 4502:4502                   Forward RIDE debugging port
# -p 8080:8080                   Forward Web Service port

docker run -v $vol -e $ride -p "4502:4502" -p "8080:8080" dyalog/jarvis