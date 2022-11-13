async function makePostRequest(url = '', data = {}) {
    const response = await fetch(url, {
        method: 'POST',
        mode: 'cors',
        cache: 'no-cache',
        credentials: 'same-origin',
        headers: {
            'Content-Type': 'application/json'
        },
        redirect: 'follow',
        referrerPolicy: 'no-referrer',
        body: JSON.stringify(data)
    });
    return response.json();
}

function getFullImg(urlString) {
    if (urlString.indexOf('http://') === 0 || urlString.indexOf('https://') === 0) {
        return urlString;
    }

    return rootUrl + urlString;
}

function preloadImage(url) {
    let img = new Image();
    img.src = url;
}

var usernameTokenCache = '';
var webSocketClient;
var currentGamePlayers = [];
var localPlayer;
var gameInfo =
{
    currentQuestion: 0,
    totalQuestions: 0,
    currentLobbyMode: 0,
    currentCategoryId: null,
    currentQuestionId: null
};

var categoriesList = {};

var timerInterval =
{
    intervalObj: null,
    endTime: null,
    durationTime: 0,
    time: null
};

var timeSynchObj = {
    deltaTime: null
}

function getCurrentServerTime() {
    let currentTime = Date.now();

    if (timeSynchObj.deltaTime) {
        currentTime += timeSynchObj.deltaTime;
    }

    return currentTime;
}

function timerIntervalFunction() {
    let currentTime = getCurrentServerTime();
    let deltaTime = timerInterval.endTime - currentTime;
    let percent = (deltaTime > 0 ? ((deltaTime / timerInterval.durationTime) * 100) : 0) | 0;

    timerInterval.time.setAttribute('aria-valuenow', percent);
    timerInterval.time.style.width = `${percent}%`;

    if (percent <= 0)
        clearInterval(timerInterval.intervalObj);
}

function addSpanBadgeToButton(button, text) {
    let spanElement = document.createElement('span');
    spanElement.classList.add('badge', 'bg-secondary', 'ms-2', 'mt-2');
    spanElement.innerText = text;
    button.appendChild(spanElement);
}

function fillPlayerPointsTable(tableBody, playersRankings) {
    tableBody.innerHTML = '';

    playersRankings.sort((a, b) => {
        return b.points - a.points;
    });

    let currentPlace = 1;
    let myPlace = currentPlace;

    for (let i = 0; i < playersRankings.length; i++) {
        const playerRank = playersRankings[i];
        let rowPlayerRank = document.createElement('tr');
        rowPlayerRank.dataset.username = playerRank.playerName;

        let incrementPlace = i != 0 && playersRankings[i].points != playersRankings[i - 1].points;

        if (incrementPlace) {
            currentPlace = currentPlace + 1;
        }

        let headerRow = document.createElement('th');
        headerRow.setAttribute('scope', 'row');
        headerRow.innerText = (incrementPlace || i == 0) ? currentPlace : '';

        let nameRow = document.createElement('td');
        nameRow.innerText = playerRank.playerName;

        let pointsRow = document.createElement('td');
        pointsRow.innerText = parseInt(playerRank.points);

        if (playerRank.playerName == localPlayer.playerName) {
            myPlace = currentPlace;
            nameRow.classList.add('owner-name');
        }

        if (playerRank.playerName == 'b3akers') {
            nameRow.classList.add('creator-name');
        }

        rowPlayerRank.appendChild(headerRow);
        rowPlayerRank.appendChild(nameRow);
        rowPlayerRank.appendChild(pointsRow);

        tableBody.appendChild(rowPlayerRank);

    }

    return { myPlace: myPlace, maxPlace: currentPlace };
}

function parseJwt(token) {
    var base64Url = token.split('.')[1];
    var base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    var jsonPayload = decodeURIComponent(window.atob(base64).split('').map(function (c) {
        return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
    }).join(''));

    return JSON.parse(jsonPayload);
}

function onInit() {
    const queryString = window.location.search;
    const urlParams = new URLSearchParams(queryString);
    if (urlParams.has('inviteCode')) {
        document.getElementById('gameInviteCode').value = urlParams.get('inviteCode');
    }

    if (urlParams.has('username_token')) {
        window.localStorage.setItem('username_token', urlParams.get('username_token'));
        window.location = window.location.pathname;
        return;
    }

    const usernameToken = window.localStorage.getItem('username_token');
    if (usernameToken) {
        try {
            const parsedToken = parseJwt(usernameToken);
            document.getElementById('userName').value = parsedToken.username;
            usernameTokenCache = usernameToken;
            document.getElementById('twitchAuth').classList.add('d-none');
        } catch (error) {

        }
    }

    var forms = document.querySelectorAll('.needs-validation')

    Array.prototype.slice.call(forms)
        .forEach(function (form) {
            form.addEventListener('submit', function (event) {

                event.preventDefault()
                event.stopPropagation()

                if (form.checkValidity()) {
                    switch (event.submitter.dataset.buttontype) {
                        case 'joinGame':
                            joinGame();
                            break;
                        case 'createGame':
                            createGame();
                            break;
                    }
                }

                form.classList.add('was-validated');
            }, false)
        });

    document.getElementById('changeLobbyMode').addEventListener('click', function (event) {
        webSocketClient.send(JSON.stringify(
            {
                type: 'change_lobby_mode',
                value: JSON.stringify({
                    lobbyMode: gameInfo.currentLobbyMode == 0 ? 1 : 0
                })
            })
        );
    });

    document.getElementById('startGameButton').addEventListener('click', function (event) {
        const form = document.getElementById('gameSettingsContainer').querySelector('form');
        if (form.checkValidity()) {
            const categoryList = document.getElementById('settingCategoryList');
            const excludedCategoriesList = [];

            categoryList.querySelectorAll('input').forEach(x => {
                if (!x.checked)
                    excludedCategoriesList.push(x.value)
            });

            window.localStorage.setItem('excludedCategories', JSON.stringify(excludedCategoriesList));

            webSocketClient.send(JSON.stringify(
                {
                    type: 'start_game',
                    value: JSON.stringify({
                        settingsRoundCount: document.getElementById('settingsRoundCount').value,
                        questionsPerCategoryCount: document.getElementById('settingsQuestionsPerCategory').value,
                        settingsTimeForQuestion: document.getElementById('settingsTimeForQuestion').value,
                        settingsCategoryPerSelection: document.getElementById('settingsCategoryPerSelection').value,
                        settingsGameMode: document.getElementById('settingsGameMode').value,
                        excludedCategoriesList: excludedCategoriesList
                    })
                })
            );
        }
        form.classList.add('was-validated');
    });

    document.getElementById('returnToLobby').addEventListener('click', function (event) {
        document.querySelectorAll('.page-container').forEach(x => x.classList.add('d-none'));
        document.getElementById('lobbyContainer').classList.remove('d-none');
    });

    const tokenValue = window.sessionStorage.getItem('jwtToken');
    if (tokenValue) {
        connectWebsocket(tokenValue, 'reconnect');
    }
}

(function () {
    fetch(getCategoriesUrl)
        .then(response => response.json())
        .then(data => {
            const categoryList = document.getElementById("settingCategoryList");
            let excludedCategoriesSettings = window.localStorage.getItem('excludedCategories');
            if (excludedCategoriesSettings) {
                excludedCategoriesSettings = JSON.parse(excludedCategoriesSettings);
            }

            data.forEach(x => {
                categoriesList[x.id] = x;
                if (x.questionCount > 200) {
                    let label = document.createElement('label');
                    label.classList.add('checkbox-inline', 'ms-2');

                    let input = document.createElement('input');
                    input.setAttribute('type', 'checkbox');
                    input.checked = !excludedCategoriesSettings || !excludedCategoriesSettings.includes(x.id);
                    input.value = x.id;

                    let text = document.createTextNode(x.label);

                    label.appendChild(input);
                    label.appendChild(text);

                    categoryList.appendChild(label);
                }
            });

            onInit();
        })
        .catch((error) => {
            toastr.error('Błąd ' + error.toString());
        });
})();

function addPlayerToLobbyTable(player) {
    const tablePlayerList = document.getElementById('lobbyPlayerList');

    let newRow = tablePlayerList.insertRow();
    newRow.dataset.username = player.playerName;

    let userNameCell = document.createElement('td');
    userNameCell.classList.add('username-cell');
    userNameCell.innerText = player.playerName;

    if (player.isOwner) {
        userNameCell.classList.add('owner-name');
    } else {
        userNameCell.classList.remove('owner-name');
    }

    if (player.playerName == 'b3akers') {
        userNameCell.classList.add('creator-name');
    }

    let userActionCell = document.createElement('td');
    userActionCell.innerText = '';

    if (localPlayer.isOwner && localPlayer.playerName != player.playerName) {
        userActionCell.innerHTML = '<button type="button" onclick="transferOwnerToPlayer(this)" class="btn btn-primary mb-2">Oddaj ownera</button> <button type="button" onclick="kickPlayerFromGame(this)" class="btn btn-primary mb-2">Wyrzuc</button>';
    }

    newRow.appendChild(userNameCell);
    newRow.appendChild(userActionCell);

    document.getElementById('playersNumber').innerText = tablePlayerList.rows.length;
}

function updateLocalLobbyButtons() {
    if (localPlayer.isOwner) {
        const readyButton = document.getElementById('startGameButton');
        readyButton.innerText = (localPlayer.isReady ? 'Rozpocznij gre ✅' : 'Rozpocznij gre ❌');

        const changeLobbyModeButton = document.getElementById('changeLobbyMode');
        changeLobbyModeButton.innerText = (gameInfo.currentLobbyMode == 0 ? 'Twitch lobby ❌' : 'Twitch lobby ✅');
    }
}

function refreshLobbyButtons() {
    if (localPlayer.isOwner) {
        document.getElementById('startGameButton').classList.remove('d-none');
        document.getElementById('changeLobbyMode').classList.remove('d-none');

        if (localPlayer.isReady)
            document.getElementById('gameSettingsContainer').querySelectorAll('input').forEach(x => x.setAttribute('disabled', ''));
        else
            document.getElementById('gameSettingsContainer').querySelectorAll('input').forEach(x => x.removeAttribute('disabled'));
    } else {
        document.getElementById('startGameButton').classList.add('d-none');
        document.getElementById('changeLobbyMode').classList.add('d-none');
        document.getElementById('gameSettingsContainer').querySelectorAll('input').forEach(x => x.setAttribute('disabled', ''));
    }
}

function updateQuestionCounterLabel() {
    document.getElementById('prepareQuestionContainer').querySelector('h4').innerText = `Pytanie ${gameInfo.currentQuestion}/${gameInfo.totalQuestions} przygotuj się!`;
}

var keepAliveWebSocketInterval;
function keepAliveWebSocket() {
    webSocketClient.send('#1');
}

function fillCategorySelectionTable(categories) {
    const list = document.getElementById('categoryList');
    list.innerHTML = '';

    for (let i = 0; i < categories.length; i++) {
        const categoryId = categories[i];
        const category = categoriesList[categoryId];

        let mainDiv = document.createElement('div');
        mainDiv.dataset.target = categoryId;
        mainDiv.classList.add('category-selection');
        mainDiv.classList.add('col-md-2');

        let imgDiv = document.createElement('div');
        imgDiv.classList.add('text-center');

        let imgElement = document.createElement('img');
        imgElement.src = getFullImg(category.icon);
        imgElement.width = 150;
        imgElement.height = 150;

        let textDiv = document.createElement('div');
        textDiv.classList.add('text-white', 'text-center', 'rounded-pill', 'p-2');
        textDiv.style.backgroundColor = category.color;
        textDiv.innerText = category.label;

        imgDiv.appendChild(imgElement);

        mainDiv.appendChild(imgDiv);
        mainDiv.appendChild(textDiv);

        mainDiv.addEventListener('click', function (event) {
            if (document.querySelector('.category-selection[selected]'))
                return;

            const targetElement = event.currentTarget;

            webSocketClient.send(JSON.stringify(
                {
                    type: 'category_vote',
                    value: JSON.stringify({
                        categoryId: targetElement.dataset.target
                    })
                })
            );

        });

        list.appendChild(mainDiv);
    }
}

function setProgressTimer(time, startTime, duration) {
    clearInterval(timerInterval.intervalObj);

    time.setAttribute('aria-valuenow', '100');
    time.style.width = '100%';

    timerInterval.durationTime = duration;
    timerInterval.endTime = startTime + timerInterval.durationTime;
    timerInterval.time = time;
    timerInterval.intervalObj = setInterval(timerIntervalFunction, 50);
}

function setupCategorySelectionStage(packetValue) {
    fillCategorySelectionTable(packetValue.categories);

    const container = document.getElementById('categorySelectionContainer');
    const time = container.querySelector('.progress-bar');

    setProgressTimer(time, packetValue.categoryStartTime, packetValue.categories.length > 0 ? (document.getElementById('settingsTimeForQuestion').value * 1000) : 1500);

    container.querySelector('h1 span').innerText = `${packetValue.currentCategoryIndex + 1}/${packetValue.maxCategoryIndex}`;

    const playersRankings = packetValue.playersRankings;
    const tableBody = container.querySelector('table tbody');
    fillPlayerPointsTable(tableBody, playersRankings);

    if (packetValue.currentCategoryVote != '') {
        selectCategoryElement(packetValue.currentCategoryVote);
    }

    document.querySelectorAll('.page-container').forEach(x => x.classList.add('d-none'));
    container.classList.remove('d-none');
}

function selectCategoryElement(categoryId) {
    const categoryElement = document.querySelector(`.category-selection[data-target="${categoryId}"]`);
    if (categoryElement) {
        categoryElement.setAttribute('selected', '');
    }
}

function setupCategoryVoteEnd(packetValue) {
    packetValue.preloadImages.forEach(x => preloadImage(getFullImg(x)));

    gameInfo.currentQuestion = packetValue.questionIndex + 1;
    gameInfo.totalQuestions = packetValue.questionsCount;

    if (packetValue.category != '')
        gameInfo.currentCategoryId = packetValue.category;
    else
        gameInfo.currentCategoryId = Object.values(categoriesList).find(x => x.label == 'Wszystko').id;

    const container = document.getElementById('prepareQuestionContainer');
    const category = categoriesList[gameInfo.currentCategoryId];

    container.querySelector('img').src = getFullImg(category.icon);
    container.querySelector('h1').innerText = category.label;
    updateQuestionCounterLabel();

    const tableHeader = container.querySelector('table thead tr');
    const tableBody = container.querySelector('table tbody');
    tableBody.innerHTML = '';
    tableHeader.innerHTML = '';

    let usernameHeader = document.createElement('th');
    usernameHeader.setAttribute('scope', 'col');
    usernameHeader.innerText = 'Nazwa użytkownika';
    tableHeader.appendChild(usernameHeader);

    for (let i = 0; i < packetValue.questionsCount; i++) {
        let questionIndexHeader = document.createElement('th');
        questionIndexHeader.setAttribute('scope', 'col');
        questionIndexHeader.innerText = (i + 1);
        tableHeader.appendChild(questionIndexHeader);
    }

    for (let i = 0; i < currentGamePlayers.length; i++) {
        const currentPlayer = currentGamePlayers[i];

        let tableRow = document.createElement('tr');
        tableRow.dataset.username = currentPlayer.playerName;

        let usernameRow = document.createElement('th');
        usernameRow.setAttribute('scope', 'row');
        usernameRow.innerText = currentPlayer.playerName;

        if (currentPlayer == localPlayer) {
            usernameRow.classList.add('owner-name');
        }

        if (currentPlayer.playerName == 'b3akers') {
            usernameRow.classList.add('creator-name');
        }

        tableRow.appendChild(usernameRow);

        for (let y = 0; y < packetValue.questionsCount; y++) {
            let questionAnswerRow = document.createElement('td');
            questionAnswerRow.innerText = '●';

            tableRow.appendChild(questionAnswerRow);
        }

        tableBody.appendChild(tableRow);
    }
}

function setupQuestionContainer(questionData) {
    gameInfo.currentQuestionId = questionData.id;

    const container = document.getElementById('questionContainer');
    const imageDiv = container.querySelector('.question-img');

    container.querySelector('h1').innerText = questionData.text;

    if (questionData.image) {
        imageDiv.removeAttribute('disabled');
        imageDiv.querySelector('img').src = getFullImg(questionData.image);
    } else {
        imageDiv.setAttribute('disabled', '');
    }

    const time = container.querySelector('.progress-bar');

    setProgressTimer(time, questionData.questionStartTime, document.getElementById('settingsTimeForQuestion').value * 1000);

    const questionTable = container.querySelector('.question-table');
    questionTable.innerHTML = '';

    let answers = questionData.answers;
    for (let i = 0; i < answers.length; i++) {
        let answer = answers[i];

        let mainDiv = document.createElement('div');
        mainDiv.classList.add('row', 'justify-content-center');

        let button = document.createElement('button');
        button.setAttribute('type', 'button');
        button.classList.add('btn', 'btn-question', 'mb-2', 'ms-2');
        button.dataset.target = answer.id;

        button.addEventListener('click', function (event) {
            const targetElement = event.currentTarget;
            const answerId = targetElement.dataset.target;

            webSocketClient.send(JSON.stringify(
                {
                    type: 'question_answer',
                    value: JSON.stringify({
                        answerId: answerId,
                        questionId: gameInfo.currentQuestionId
                    })
                })
            );
        });

        let answerText = document.createElement('h4');
        answerText.innerText = answer.text;

        button.appendChild(answerText);
        mainDiv.appendChild(button);
        questionTable.appendChild(mainDiv);
    }
}

function updateLobbySettingsValues(gameSettings) {
    document.getElementById('settingsRoundCount').value = gameSettings.roundCount;
    document.getElementById('settingsQuestionsPerCategory').value = gameSettings.questionsPerCategory;
    document.getElementById('settingsTimeForQuestion').value = gameSettings.timeSecondForQuestion;
    document.getElementById('settingsCategoryPerSelection').value = gameSettings.categoryPerSelectionCount;
    document.getElementById('settingsGameMode').value = gameSettings.gameMode;

    if (!localPlayer.isOwner) {
        const categoryList = document.getElementById('settingCategoryList');
        categoryList.querySelectorAll('input').forEach(x => {
            x.checked = !gameSettings.excludedCategoriesList.includes(x.value);
        });
    }
}

function transferOwnerToPlayer(target) {
    webSocketClient.send(JSON.stringify(
        {
            type: 'transfer_owner',
            value: JSON.stringify({
                playerName: target.parentElement.parentElement.dataset.username
            })
        })
    );
}

function kickPlayerFromGame(target) {
    webSocketClient.send(JSON.stringify(
        {
            type: 'game_kick',
            value: JSON.stringify({
                playerName: target.parentElement.parentElement.dataset.username
            })
        })
    );

    document.querySelectorAll(`tr[data-username="${target.parentElement.parentElement.dataset.username}"]`).forEach(x => x.remove());
}

function connectWebsocket(jwtToken, loginType) {
    clearInterval(keepAliveWebSocketInterval);

    window.sessionStorage.setItem('jwtToken', jwtToken);

    webSocketClient = new WebSocket(websocketGameUrl);
    webSocketClient.onopen = function (event) {

        keepAliveWebSocketInterval = setInterval(keepAliveWebSocket, 5000);

        webSocketClient.send(JSON.stringify(
            {
                type: loginType,
                value: JSON.stringify({
                    token: jwtToken
                })
            })
        );
    };

    webSocketClient.onclose = function (event) {
        clearInterval(keepAliveWebSocketInterval);
        if (event.code == 1006) {
            const tokenValue = window.sessionStorage.getItem('jwtToken');
            if (tokenValue)
                connectWebsocket(tokenValue, 'reconnect');
        } else if (event.code == 3300) {
            window.sessionStorage.removeItem('jwtToken');
            window.location.reload();
        }
    };

    webSocketClient.onmessage = function (event) {
        if (event.data == '#2')
            return;

        let packetHeader = JSON.parse(event.data);

        if (packetHeader.type == 'lobby') {
            let lobbyPacketData = packetHeader.value;

            timeSynchObj.deltaTime = packetHeader.value.serverTime - Date.now();
            document.getElementById('inviteCode').value = lobbyPacketData.inviteCode;

            const tablePlayerList = document.getElementById('lobbyPlayerList');
            tablePlayerList.innerHTML = '';

            gameInfo.currentLobbyMode = packetHeader.value.lobbyMode;
            currentGamePlayers = [];

            for (let i = 0; i < lobbyPacketData.lobbyPlayers.length; i++) {
                let player = lobbyPacketData.lobbyPlayers[i];
                currentGamePlayers.push({
                    playerName: player.playerName,
                    isReady: player.isReady,
                    isOwner: player.isOwner
                });
            }

            localPlayer = currentGamePlayers.find(x => x.playerName == lobbyPacketData.playerName);

            for (let i = 0; i < currentGamePlayers.length; i++) {
                addPlayerToLobbyTable(currentGamePlayers[i]);
            }

            refreshLobbyButtons();
            updateLocalLobbyButtons();
            updateLobbySettingsValues(lobbyPacketData.settings);

            document.querySelectorAll('.page-container').forEach(x => x.classList.add('d-none'));
            document.getElementById('lobbyContainer').classList.remove('d-none');
        } else if (packetHeader.type == 'lobby_join') {
            let player = packetHeader.value;

            addPlayerToLobbyTable(player);
            currentGamePlayers.push({
                playerName: player.playerName,
                isReady: player.isReady,
                isOwner: player.isOwner
            });
        } else if (packetHeader.type == 'lobby_ready') {
            let player = packetHeader.value;
            const playerObject = currentGamePlayers.find(x => x.playerName == player.playerName);
            if (playerObject) {
                playerObject.isReady = player.isReady;
                const tablePlayerList = document.getElementById('lobbyPlayerList');

                if (playerObject.isOwner) {
                    if (!playerObject.isReady) {
                        for (let i = 0; i < currentGamePlayers.length; i++) {
                            const player = currentGamePlayers[i];
                            player.isReady = false;
                        }
                    } else {
                        updateLobbySettingsValues(packetHeader.value.settings);
                    }
                }

                if (localPlayer.playerName == playerObject.playerName || playerObject.isOwner) {
                    refreshLobbyButtons();
                    updateLocalLobbyButtons();
                }
            }
        } else if (packetHeader.type == 'player_leave') {
            let player = packetHeader.value;
            const playerObjectIndex = currentGamePlayers.findIndex(x => x.playerName == player.playerName);
            const tablePlayerList = document.getElementById('lobbyPlayerList');
            if (playerObjectIndex >= 0) {
                document.querySelectorAll(`tr[data-username="${player.playerName}"]`).forEach(x => x.remove());
                document.getElementById('playersNumber').innerText = tablePlayerList.rows.length;

                currentGamePlayers.splice(playerObjectIndex, 1);
            }
        } else if (packetHeader.type == 'owner_transfer') {
            let playerName = packetHeader.value.playerName;
            const playerObject = currentGamePlayers.find(x => x.playerName == playerName);
            const currentOwner = currentGamePlayers.find(x => x.isOwner);
            if (currentOwner) {
                currentOwner.isOwner = false;
            }
            const tablePlayerList = document.getElementById('lobbyPlayerList');
            if (playerObject) {
                playerObject.isOwner = true;

                for (let i = 0; i < currentGamePlayers.length; i++) {
                    const player = currentGamePlayers[i];
                    player.isReady = false;

                    if (player.isOwner && player.playerName != playerName) {
                        player.isOwner = false;
                    }

                    const tableEntry = tablePlayerList.querySelector(`tr[data-username="${player.playerName}"]`);
                    if (tableEntry) {

                        if (player.isOwner) {
                            tableEntry.children[0].classList.add('owner-name');
                        } else {
                            tableEntry.children[0].classList.remove('owner-name');
                        }

                        tableEntry.children[1].innerText = '';

                        if (localPlayer.isOwner && localPlayer.playerName != player.playerName) {
                            tableEntry.children[1].innerHTML = '<button type="button" onclick="transferOwnerToPlayer(this)" class="btn btn-primary mb-2">Oddaj ownera</button> <button type="button" onclick="kickPlayerFromGame(this)" class="btn btn-primary mb-2">Wyrzuc</button>';
                        }
                    }
                }

                if (localPlayer == playerObject || localPlayer == currentOwner) {
                    refreshLobbyButtons();
                }

                updateLocalLobbyButtons();
            }
        } else if (packetHeader.type == 'category_vote_start') {
            setupCategorySelectionStage(packetHeader.value);
        } else if (packetHeader.type == 'category_vote') {
            selectCategoryElement(packetHeader.value.categoryId);
        } else if (packetHeader.type == 'category_vote_end') {
            setupCategoryVoteEnd(packetHeader.value);

            document.querySelectorAll('.page-container').forEach(x => x.classList.add('d-none'));
            document.getElementById('prepareQuestionContainer').classList.remove('d-none');
        } else if (packetHeader.type == 'question_start') {
            setupQuestionContainer(packetHeader.value);

            document.querySelectorAll('.page-container').forEach(x => x.classList.add('d-none'));
            document.getElementById('questionContainer').classList.remove('d-none');
        } else if (packetHeader.type == 'question_timeout_players') {
            if (packetHeader.value.questionId != gameInfo.currentQuestionId)
                return;

            const containerTable = document.getElementById('prepareQuestionContainer');

            if (packetHeader.value.skippedPlayers) {
                const skippedPlayers = packetHeader.value.skippedPlayers;
                for (let i = 0; i < skippedPlayers.length; i++) {
                    const playerRow = containerTable.querySelector(`table tbody tr[data-username="${skippedPlayers[i]}"]`);
                    if (playerRow) {
                        playerRow.children[gameInfo.currentQuestion].innerText = '❌';
                    }
                }
            }

        } else if (packetHeader.type == 'question_answer') {
            if (packetHeader.value.questionId != gameInfo.currentQuestionId)
                return;

            const container = document.getElementById('questionContainer');
            const questionTable = container.querySelector('.question-table');
            const correctAnswer = packetHeader.value.correctAnswer;

            questionTable.querySelectorAll('.btn-question').forEach(x => x.classList.add('question-disabled'));

            const correctAnswerButton = questionTable.querySelector(`button[data-target="${correctAnswer}"]`);
            const containerTable = document.getElementById('prepareQuestionContainer');

            if (correctAnswerButton) {
                correctAnswerButton.setAttribute('correct', '');
            }

            if (packetHeader.value.answerId) {
                if (packetHeader.value.answerId == correctAnswer) {
                    addSpanBadgeToButton(correctAnswerButton, localPlayer.playerName);
                } else {
                    const wrongAnswerButton = questionTable.querySelector(`button[data-target="${packetHeader.value.answerId}"]`);
                    if (wrongAnswerButton) {
                        wrongAnswerButton.setAttribute('wrong', '');
                        addSpanBadgeToButton(wrongAnswerButton, localPlayer.playerName);
                    }
                }

                const playerRow = containerTable.querySelector(`table tbody tr[data-username="${localPlayer.playerName}"]`);
                if (playerRow) {
                    playerRow.children[gameInfo.currentQuestion].innerText = packetHeader.value.answerId == correctAnswer ? '✅' : '❌';
                }
            }

            if (packetHeader.value.skippedPlayers) {
                const skippedPlayers = packetHeader.value.skippedPlayers;
                for (let i = 0; i < skippedPlayers.length; i++) {
                    const playerRow = containerTable.querySelector(`table tbody tr[data-username="${skippedPlayers[i]}"]`);
                    if (playerRow) {
                        playerRow.children[gameInfo.currentQuestion].innerText = '❌';
                    }
                }
            }

            const otherPlayer = packetHeader.value.otherPlayers;

            for (let i = 0; i < otherPlayer.length; i++) {
                const playerAnswer = otherPlayer[i];
                const answerButton = questionTable.querySelector(`button[data-target="${playerAnswer.answerId}"]`);

                if (answerButton) {
                    if (playerAnswer.answerId != correctAnswer) {
                        answerButton.setAttribute('wrong', '');
                    }
                    addSpanBadgeToButton(answerButton, playerAnswer.playerName);
                }

                const playerRow = containerTable.querySelector(`table tbody tr[data-username="${playerAnswer.playerName}"]`);
                if (playerRow) {
                    playerRow.children[gameInfo.currentQuestion].innerText = playerAnswer.answerId == correctAnswer ? '✅' : '❌';
                }
            }
        } else if (packetHeader.type == 'player_answer_question') {
            if (packetHeader.value.questionId != gameInfo.currentQuestionId)
                return;

            const container = document.getElementById('questionContainer');
            const questionTable = container.querySelector('.question-table');
            const answerButton = questionTable.querySelector(`button[data-target="${packetHeader.value.answerId}"]`);
            if (answerButton) {
                if (!answerButton.hasAttribute('correct'))
                    answerButton.setAttribute('wrong', '');

                addSpanBadgeToButton(answerButton, packetHeader.value.playerName);
            }

            const containerTable = document.getElementById('prepareQuestionContainer');
            const playerRow = containerTable.querySelector(`table tbody tr[data-username="${packetHeader.value.playerName}"]`);
            if (playerRow) {
                playerRow.children[gameInfo.currentQuestion].innerText = packetHeader.value.isCorrect ? '✅' : '❌';
            }

        } else if (packetHeader.type == 'preare_next_question') {
            gameInfo.currentQuestion = packetHeader.value.questionIndex + 1;

            updateQuestionCounterLabel();

            document.querySelectorAll('.page-container').forEach(x => x.classList.add('d-none'));
            document.getElementById('prepareQuestionContainer').classList.remove('d-none');
        } else if (packetHeader.type == 'round_end') {
            document.getElementById('prepareQuestionContainer').querySelector('h4').innerText = 'Wyniki:';

            document.querySelectorAll('.page-container').forEach(x => x.classList.add('d-none'));
            document.getElementById('prepareQuestionContainer').classList.remove('d-none');
        } else if (packetHeader.type == 'game_finished') {
            const container = document.getElementById('endGameResultContainer');

            const playersRankings = packetHeader.value.playersRankings;
            const tableBody = container.querySelector('table tbody');
            const { myPlace, maxPlace } = fillPlayerPointsTable(tableBody, playersRankings);

            const tablePlayerList = document.getElementById('lobbyPlayerList');
            tablePlayerList.innerHTML = '';

            for (let i = 0; i < currentGamePlayers.length; i++) {
                const player = currentGamePlayers[i];
                player.isReady = false;

                addPlayerToLobbyTable(player);
            }

            refreshLobbyButtons();
            if (localPlayer.isOwner) {
                const readyButton = document.getElementById('startGameButton');
                readyButton.innerText = (localPlayer.isReady ? 'Rozpocznij gre ✅' : 'Rozpocznij gre ❌');
            }

            container.querySelector('h1 span').innerText = `${myPlace}/${maxPlace}`;

            document.querySelectorAll('.page-container').forEach(x => x.classList.add('d-none'));
            container.classList.remove('d-none');
        } else if (packetHeader.type == 'game_timeout') {
            document.querySelectorAll('.page-container').forEach(x => x.classList.add('d-none'));
            document.getElementById('startContainer').classList.remove('d-none');
        } else if (packetHeader.type == 'lobby_mode_changed') {
            gameInfo.currentLobbyMode = packetHeader.value.lobbyMode;
            updateLocalLobbyButtons();
        } else if (packetHeader.type == 'reconnect') {
            const lobbyPlayers = packetHeader.value.lobbyPlayers;
            currentGamePlayers = [];
            for (let i = 0; i < lobbyPlayers.length; i++) {
                let player = lobbyPlayers[i];
                currentGamePlayers.push({
                    playerName: player.playerName,
                    isReady: player.isReady,
                    isOwner: player.isOwner
                });
            }
            localPlayer = currentGamePlayers.find(x => x.playerName == packetHeader.value.playerName);

            document.getElementById('inviteCode').value = packetHeader.value.inviteCode;
            timeSynchObj.deltaTime = packetHeader.value.serverTime - Date.now();
            gameInfo.currentLobbyMode = packetHeader.value.lobbyMode;

            updateLobbySettingsValues(packetHeader.value.settings);

            const gameState = packetHeader.value.gameState;

            if (gameState == 'CategorySelection') {
                setupCategorySelectionStage(packetHeader.value);
            } else if (gameState == 'CategoryInGame') {
                setupCategoryVoteEnd(packetHeader.value);

                const containerTable = document.getElementById('prepareQuestionContainer');
                for (let i = 0; i < lobbyPlayers.length; i++) {
                    const player = lobbyPlayers[i];
                    const playerRow = containerTable.querySelector(`table tbody tr[data-username="${player.playerName}"]`);
                    if (playerRow) {
                        for (let y = 0; y < player.answerStatus.length; y++)
                            playerRow.children[y + 1].innerText = player.answerStatus[y] ? '✅' : '❌';
                    }
                }

                if (packetHeader.value.gameStateSub == 'PrepareForQuestions'
                    || packetHeader.value.gameStateSub == 'RoundEnd') {

                    let showPrepareQuestionContainer = false;

                    if (packetHeader.value.gameStateSub == 'PrepareForQuestions') {
                        updateQuestionCounterLabel();
                        showPrepareQuestionContainer = true;
                    } else if (packetHeader.value.gameStateSub == 'RoundEnd') {
                        document.getElementById('prepareQuestionContainer').querySelector('h4').innerText = 'Wyniki:';
                        showPrepareQuestionContainer = true;
                    }

                    if (showPrepareQuestionContainer) {
                        document.querySelectorAll('.page-container').forEach(x => x.classList.add('d-none'));
                        containerTable.classList.remove('d-none');
                    }
                }

                if (packetHeader.value.gameStateSub == 'QuestionAnswered' || packetHeader.value.gameStateSub == 'QuestionAnswering') {
                    setupQuestionContainer(packetHeader.value.questionData);

                    const questionContainer = document.getElementById('questionContainer');
                    const correctAnswer = packetHeader.value.correntAnswerId;
                    if (correctAnswer != '') {
                        const questionTable = questionContainer.querySelector('.question-table');
                        questionTable.querySelectorAll('.btn-question').forEach(x => x.classList.add('question-disabled'));

                        const correctAnswerButton = questionTable.querySelector(`button[data-target="${correctAnswer}"]`);
                        if (correctAnswerButton) {
                            correctAnswerButton.setAttribute('correct', '');

                            if (correctAnswer == packetHeader.value.questionAnswerId) {
                                addSpanBadgeToButton(correctAnswerButton, localPlayer.playerName);
                            }
                        }

                        for (let i = 0; i < lobbyPlayers.length; i++) {
                            const player = lobbyPlayers[i];
                            if (player.answerId == '')
                                continue;

                            if (player.answerId == correctAnswer && player.playerName == localPlayer.playerName)
                                continue;

                            const userAnswerButton = questionTable.querySelector(`button[data-target="${player.answerId}"]`);
                            if (userAnswerButton) {
                                if (player.answerId != correctAnswer)
                                    userAnswerButton.setAttribute('wrong', '');

                                addSpanBadgeToButton(userAnswerButton, player.playerName);
                            }
                        }
                    }

                    document.querySelectorAll('.page-container').forEach(x => x.classList.add('d-none'));
                    questionContainer.classList.remove('d-none');
                }
            }
        }
    }
}

function createGame() {
    makePostRequest(createGameUrl, { username: document.getElementById('userName').value })
        .then(data => {
            if (data.error) {
                toastr.error('Nieznany błąd');
            } else {
                connectWebsocket(data.token, 'login');
            }
        })
        .catch((error) => {
            toastr.error('Błąd ' + error.toString());
        });
}

function joinGame() {
    makePostRequest(joinGameUrl, { username: document.getElementById('userName').value, invitecode: document.getElementById('gameInviteCode').value, token: usernameTokenCache })
        .then(data => {
            if (data.error) {
                if (data.error === 'game_not_found') {
                    toastr.error('Nie znaleziono takiej gry');
                } else if (data.error === 'invalid_username') {
                    toastr.error('Nazwa użytkownika jest juz zajęta');
                } else if (data.error === 'game_running') {
                    toastr.error('Gra już trwa');
                } else if (data.error === 'game_lobby_full') {
                    toastr.error('Lobby jest już pełne');
                } else if (data.error === 'invalid_username_token') {
                    toastr.error('Lobby tylko dla użytkowników połaczonych z twitch.tv');
                    window.localStorage.removeItem('username_token');
                    usernameTokenCache = '';
                    document.getElementById('twitchAuth').classList.remove('d-none');
                } else {
                    toastr.error('Nieznany błąd');
                }
            } else {
                connectWebsocket(data.token, 'login');
            }
        })
        .catch((error) => {
            toastr.error('Błąd ' + error.toString());
        });
}

setInterval(function () {
    const tablePlayerList = document.getElementById('lobbyPlayerList');
    document.getElementById('playersNumber').innerText = tablePlayerList.rows.length;
}, 5000);