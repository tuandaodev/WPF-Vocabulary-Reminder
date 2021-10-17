var _Environments = {
    // BASE_URL: 'http://192.168.1.93:5000',
    BASE_URL: 'https://vocabularyreminderapi.azurewebsites.net',
    API_KEY: ''
}

function getEnvironment() {
    // ...now return the correct environment
    return _Environments;
}

var Environment = getEnvironment()
module.exports = Environment