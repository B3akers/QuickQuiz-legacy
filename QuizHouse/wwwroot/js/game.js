var websocketClient;

var keepAliveWebSocketInterval;
function keepAliveWebSocket() {
    websocketClient.send('#1');
}

(function () {
    websocketClient = new WebSocket(gameWebsocketUrl);

    websocketClient.onopen = function (event) {
        keepAliveWebSocketInterval = setInterval(keepAliveWebSocket, 5000);

        
    };

    websocketClient.onclose = function (event) {
        clearInterval(keepAliveWebSocketInterval);
    };
})();