# Invoke request to involute web service running on port 8080 (see start.ps1)

$hdrs = @{'Content-Type' = 'application/json'}
$uri = "http://localhost:8080/Involute"
$body = "7"

Invoke-WebRequest -URI $uri -Headers $hdrs -Body $body -Method Post