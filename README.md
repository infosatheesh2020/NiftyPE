# Nifty PE

This project deploys an Azure function App and polls the URL for Nifty PE values and provide insights of next 3-year returns of Nifty based on PE levels.

There is additional insights on Buy/Sell decision based on this information.

It also uses Twilio's SendGrid on Azure to send the email.

## Environment Variables to be set in Function App

  - api-key: Twilio's API key
  - to-email1: Receiver 1 email
  - to-email2: Receiver 2 email

## Function Details

  - NiftyPEWeeklyTimer - Runs the function as weekly timer trigger on every Monday 8 AM
  - NiftyPE - Runs function as HTTP trigger on demand