var webSocketClient;
var categoriesList = {};
var currentGamePlayers = [];
var localPlayer;
var gameInfo = {
    currentQuestion: 0,
    totalQuestions: 0,
    currentCategoryId: null,
    currentQuestionId: null
};
var timerInterval = {
    intervalObj: null,
    endTime: null,
    durationTime: 0,
    time: null
};
var timeSynchObj = {
    deltaTime: null
}

var keepAliveWebSocketInterval;

function keepAliveWebSocket() {
    webSocketClient.send('#1');
}

function initWebsocketConnection() {
    webSocketClient = new WebSocket(gameWebsocketUrl);

    webSocketClient.onopen = function (event) {
        keepAliveWebSocketInterval = setInterval(keepAliveWebSocket, 5000);
    };

    webSocketClient.onmessage = function (event) {
        if (event.data == '#2')
            return;

        let packetHeader = JSON.parse(event.data);
        let packetValue = packetHeader.value;

        if (packetHeader.type == 'game_restore') {
            timeSynchObj.deltaTime = packetValue.serverTime - Date.now();
            loadPlayers(packetValue.players, packetValue.playerId);

            if (packetValue.gameState != 'CategorySelection') {
                setupCategoryVoteEnd(packetValue);

                const containerTable = document.getElementById('prepareQuestionContainer');
                for (let i = 0; i < packetValue.players.length; i++) {
                    const player = packetValue.players[i];
                    const playerRow = containerTable.querySelector(`table tbody tr[data-user-id="${player.id}"]`);
                    if (playerRow) {
                        for (let y = 0; y < player.answersData.length; y++)
                            playerRow.children[y + 1].innerText = player.answersData[y].item1 ? '✅' : '❌';
                    }
                }

                if (packetValue.gameState == 'PrepareForQuestions'
                    || packetValue.gameState == 'RoundEnd') {

                    let showPrepareQuestionContainer = false;

                    if (packetValue.gameState == 'PrepareForQuestions') {
                        updateQuestionCounterLabel();
                        showPrepareQuestionContainer = true;
                    } else if (packetValue.gameState == 'RoundEnd') {
                        containerTable.querySelector('h4').innerText = 'Wyniki:';
                        showPrepareQuestionContainer = true;
                    }

                    if (showPrepareQuestionContainer) {
                        showContainerByName('prepareQuestionContainer');
                    }
                }

                if (packetValue.gameState == 'QuestionAnswered' || packetValue.gameState == 'QuestionAnswering') {
                    setupQuestionContainer(packetValue.questionData);

                    const questionContainer = document.getElementById('questionContainer');
                    const correctAnswer = packetValue.correntAnswerIndex;
                    if (correctAnswer != -1) {
                        const questionTable = questionContainer.querySelector('.question-table');
                        questionTable.querySelectorAll('.btn-question').forEach(x => x.classList.add('question-disabled'));

                        const correctAnswerButton = questionTable.querySelector(`button[data-target="${correctAnswer}"]`);
                        if (correctAnswerButton) {
                            correctAnswerButton.setAttribute('correct', '');

                            if (correctAnswer == packetValue.questionAnswerIndex) {
                                addSpanBadgeToButton(correctAnswerButton, localPlayer.name);
                            }
                        }

                        for (let i = 0; i < packetValue.players.length; i++) {
                            const player = packetValue.players[i];
                            if (player.answerIndex == -1)
                                continue;

                            if (player.answerIndex == correctAnswer && player.id == localPlayer.id)
                                continue;

                            const userAnswerButton = questionTable.querySelector(`button[data-target="${player.answerIndex}"]`);
                            if (userAnswerButton) {
                                if (player.answerIndex != correctAnswer)
                                    userAnswerButton.setAttribute('wrong', '');

                                addSpanBadgeToButton(userAnswerButton, getPlayerName(player.id));
                            }
                        }
                    }

                    showContainerByName("questionContainer");
                }
            }
        } else if (packetHeader.type == 'category_vote_end') {
            setupCategoryVoteEnd(packetValue);
            showContainerByName('prepareQuestionContainer');
        } else if (packetHeader.type == 'question_start') {
            setupQuestionContainer(packetValue);
            showContainerByName('questionContainer');
        } else if (packetHeader.type == 'preare_next_question') {
            gameInfo.currentQuestion = packetValue.questionIndex + 1;
            updateQuestionCounterLabel();
            showContainerByName('prepareQuestionContainer');
        } else if (packetHeader.type == 'round_end') {
            document.getElementById('prepareQuestionContainer').querySelector('h4').innerText = 'Wyniki:';
            showContainerByName('prepareQuestionContainer');
        } else if (packetHeader.type == 'question_answer') {
            if (packetValue.questionId != gameInfo.currentQuestionId)
                return;

            const container = document.getElementById('questionContainer');
            const questionTable = container.querySelector('.question-table');
            const correctAnswer = packetValue.correctAnswer;

            questionTable.querySelectorAll('.btn-question').forEach(x => x.classList.add('question-disabled'));

            const correctAnswerButton = questionTable.querySelector(`button[data-target="${correctAnswer}"]`);
            const containerTable = document.getElementById('prepareQuestionContainer');

            if (correctAnswerButton) {
                correctAnswerButton.setAttribute('correct', '');
            }

            if (packetValue.answerIndex != -1) {
                if (packetValue.answerIndex == correctAnswer) {
                    addSpanBadgeToButton(correctAnswerButton, localPlayer.name);
                } else {
                    const wrongAnswerButton = questionTable.querySelector(`button[data-target="${packetValue.answerIndex}"]`);
                    if (wrongAnswerButton) {
                        wrongAnswerButton.setAttribute('wrong', '');
                        addSpanBadgeToButton(wrongAnswerButton, localPlayer.name);
                    }
                }

                const playerRow = containerTable.querySelector(`table tbody tr[data-user-id="${localPlayer.id}"]`);
                if (playerRow) {
                    playerRow.children[gameInfo.currentQuestion].innerText = packetValue.answerIndex == correctAnswer ? '✅' : '❌';
                }
            }

            if (packetValue.skippedPlayers) {
                const skippedPlayers = packetValue.skippedPlayers;
                for (let i = 0; i < skippedPlayers.length; i++) {
                    const playerRow = containerTable.querySelector(`table tbody tr[data-user-id="${skippedPlayers[i]}"]`);
                    if (playerRow) {
                        playerRow.children[gameInfo.currentQuestion].innerText = '❌';
                    }
                }
            }

            const otherPlayer = packetValue.otherPlayers;

            for (let i = 0; i < otherPlayer.length; i++) {
                const playerAnswer = otherPlayer[i];
                const answerButton = questionTable.querySelector(`button[data-target="${playerAnswer.item2}"]`);

                if (answerButton) {
                    if (playerAnswer.item2 != correctAnswer) {
                        answerButton.setAttribute('wrong', '');
                    }
                    addSpanBadgeToButton(answerButton, getPlayerName(playerAnswer.item1));
                }

                const playerRow = containerTable.querySelector(`table tbody tr[data-user-id="${playerAnswer.item1}"]`);
                if (playerRow) {
                    playerRow.children[gameInfo.currentQuestion].innerText = playerAnswer.item2 == correctAnswer ? '✅' : '❌';
                }
            }
        } else if (packetHeader.type == 'question_timeout_players') {
            if (packetValue.questionId != gameInfo.currentQuestionId)
                return;

            const containerTable = document.getElementById('prepareQuestionContainer');

            if (packetValue.skippedPlayers) {
                const skippedPlayers = packetValue.skippedPlayers;
                for (let i = 0; i < skippedPlayers.length; i++) {
                    const playerRow = containerTable.querySelector(`table tbody tr[data-user-id="${skippedPlayers[i]}"]`);
                    if (playerRow) {
                        playerRow.children[gameInfo.currentQuestion].innerText = '❌';
                    }
                }
            }
        } else if (packetHeader.type == 'player_answer_question') {
            if (packetValue.questionId != gameInfo.currentQuestionId)
                return;

            const container = document.getElementById('questionContainer');
            const questionTable = container.querySelector('.question-table');
            const answerButton = questionTable.querySelector(`button[data-target="${packetValue.answerIndex}"]`);
            if (answerButton) {
                if (!answerButton.hasAttribute('correct'))
                    answerButton.setAttribute('wrong', '');

                addSpanBadgeToButton(answerButton, getPlayerName(packetValue.playerId));
            }

            const containerTable = document.getElementById('prepareQuestionContainer');
            const playerRow = containerTable.querySelector(`table tbody tr[data-user-id="${packetValue.playerId}"]`);
            if (playerRow) {
                playerRow.children[gameInfo.currentQuestion].innerText = packetValue.isCorrect ? '✅' : '❌';
            }
        } else if (packetHeader.type == 'game_finished') {
            const container = document.getElementById('endGameResultContainer');

            const playersRankings = packetValue.playersRankings;
            const tableBody = container.querySelector('table tbody');
            const { myPlace, maxPlace } = fillPlayerPointsTable(tableBody, playersRankings);

            container.querySelector('h1 span').innerText = `${myPlace}/${maxPlace}`;

            showContainerByName("endGameResultContainer");
        }
    };

    webSocketClient.onclose = function (event) {
        clearInterval(keepAliveWebSocketInterval);
    };
}

(function () {
    fetch(getCategoriesUrl, {
        credentials: 'same-origin'
    })
        .then(response => response.json())
        .then(data => {
            data.forEach(x => {
                categoriesList[x.id] = x;
            });

            initWebsocketConnection();
        })
        .catch((error) => {
            toastr.error(error.toString());
        });
})();

function reportQuestion() {
    $.confirm({
        title: 'Zgłoś pytanie',
        content: '' +
            '<form action="" class="formName">' +
            '<div class="form-check">' +
            '<input class="form-check-input" type="radio" data-report-reason="0" name="reportReason" checked><label class="form-check-label">Nieaktualne pytanie</label>' +
            '</div>' +
            '<div class="form-check">' +
            '<input class="form-check-input" type="radio" data-report-reason="1" name="reportReason"><label class="form-check-label">Zła kategoria</label>' +
            '</div>' +
            '<div class="form-check">' +
            '<input class="form-check-input" type="radio" data-report-reason="2" name="reportReason"><label class="form-check-label">Zła odpowiedź</label>' +
            '</div>' +
            '<div class="form-check">' +
            '<input class="form-check-input" type="radio" data-report-reason="3" name="reportReason"><label class="form-check-label">Inne</label>' +
            '</div>' +
            `<input type="hidden" name="id" value="${gameInfo.currentQuestionId}" />` +
            '</form>',
        theme: 'dark',
        buttons: {
            formSubmit: {
                text: 'Zgłoś',
                action: function () {
                    const content = this.$content[0];

                    const id = content.querySelector('input[name="id"]').value.trim();
                    const reportReason = content.querySelector('input[name="reportReason"]:checked').dataset.reportReason.trim();

                    if (!id || !reportReason) {
                        toastr.error(translateCode('invalid_model'));
                        return false;
                    }

                    const data = { id: id, reportReason: parseInt(reportReason) };

                    makePostRequest(reportQuestionUrl, data)
                        .then(data => {
                            if (data.error) {
                                toastr.error(translateCode(data.error));
                                return;
                            }

                            toastr.success(translateCode(data.success));
                        })
                        .catch((error) => {
                            toastr.error(error.toString());
                        });

                    return true;
                }
            },
            cancel: function () {

            }
        }
    })
}

function shuffle(array) {
    let currentIndex = array.length, randomIndex;

    while (currentIndex != 0) {

        randomIndex = Math.floor(Math.random() * currentIndex);
        currentIndex--;

        [array[currentIndex], array[randomIndex]] = [
            array[randomIndex], array[currentIndex]];
    }

    return array;
}

function createTwitchBadge(login, displayName) {

    let a = document.createElement('a');
    a.setAttribute('target', '_blank');
    a.setAttribute('href', 'https://www.twitch.tv/' + login);

    let span = document.createElement('span');
    span.innerHTML = '<i class="d-inline-block" height="20" width="20" style="height: 20px; width: 20px;"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512"><path fill="#7212ff" d="M391.17,103.47H352.54v109.7h38.63ZM285,103H246.37V212.75H285ZM120.83,0,24.31,91.42V420.58H140.14V512l96.53-91.42h77.25L487.69,256V0ZM449.07,237.75l-77.22,73.12H294.61l-67.6,64v-64H140.14V36.58H449.07Z"></path></svg></i>';
    span.classList.add('ms-2');
    span.title = displayName;
    span.dataset.toggle = 'tooltip';
    span.dataset.placement = 'top';
    $(span).tooltip();

    a.appendChild(span);

    return a;
}

function changeTableRowPlayer(row, player) {
    if (player.color) {
        row.style.color = player.color;
    }

    player.connectionsInfo.forEach(x => {
        if (x.item1 == 'Twitch') {
            row.appendChild(createTwitchBadge(x.item2, x.item3));
        }
    });
}

function getPlayerName(playerId) {
    return currentGamePlayers.find(x => x.id == playerId).name;
}

function getPlayer(playerId) {
    return currentGamePlayers.find(x => x.id == playerId);
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
        const player = getPlayer(playerRank.playerId);

        let rowPlayerRank = document.createElement('tr');
        rowPlayerRank.dataset.userId = playerRank.playerId;

        let incrementPlace = i != 0 && playerRank.points != playersRankings[i - 1].points;

        if (incrementPlace) {
            currentPlace = currentPlace + 1;
        }

        let headerRow = document.createElement('th');
        headerRow.setAttribute('scope', 'row');
        headerRow.innerText = (incrementPlace || i == 0) ? currentPlace : '';

        let nameRow = document.createElement('td');
        nameRow.innerText = player.name;

        let pointsRow = document.createElement('td');
        pointsRow.innerText = parseInt(playerRank.points);

        changeTableRowPlayer(nameRow, player);

        rowPlayerRank.appendChild(headerRow);
        rowPlayerRank.appendChild(nameRow);
        rowPlayerRank.appendChild(pointsRow);

        tableBody.appendChild(rowPlayerRank);
    }

    return { myPlace: myPlace, maxPlace: currentPlace };
}

function addSpanBadgeToButton(button, text) {
    let spanElement = document.createElement('span');
    spanElement.classList.add('badge', 'bg-secondary', 'ms-2', 'mt-2');
    spanElement.innerText = text;
    button.appendChild(spanElement);
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

function setProgressTimer(time, startTime, duration) {
    clearInterval(timerInterval.intervalObj);

    time.setAttribute('aria-valuenow', '100');
    time.style.width = '100%';

    timerInterval.durationTime = duration;
    timerInterval.endTime = startTime + timerInterval.durationTime;
    timerInterval.time = time;
    timerInterval.intervalObj = setInterval(timerIntervalFunction, 50);
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

function showContainerByName(name) {
    document.querySelectorAll('.page-container').forEach(x => x.classList.add('d-none'));
    document.getElementById(name).classList.remove('d-none');
}

function updateQuestionCounterLabel() {
    document.getElementById('prepareQuestionContainer').querySelector('h4').innerText = `Pytanie ${gameInfo.currentQuestion}/${gameInfo.totalQuestions} przygotuj się!`;
}

function loadPlayers(playersArray, localPlayerId) {
    currentGamePlayers = [];
    for (let i = 0; i < playersArray.length; i++) {
        let player = playersArray[i];
        currentGamePlayers.push({
            id: player.id,
            name: player.username,
            connectionsInfo: player.connectionsInfo,
            color: player.color
        });
    }

    localPlayer = currentGamePlayers.find(x => x.id == localPlayerId);
    localPlayer.color = userGameSettings.myColor;
}

function setupCategoryVoteEnd(packetValue) {
    packetValue.preloadImages.forEach(x => preloadImage(getFullImg(x)));

    gameInfo.currentQuestion = packetValue.questionIndex + 1;
    gameInfo.totalQuestions = packetValue.questionsCount;
    gameInfo.currentCategoryId = packetValue.categoryId;

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
        tableRow.dataset.userId = currentPlayer.id;

        let usernameRow = document.createElement('th');
        usernameRow.setAttribute('scope', 'row');
        usernameRow.innerText = currentPlayer.name;

        changeTableRowPlayer(usernameRow, currentPlayer);

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

    setProgressTimer(time, questionData.questionStartTime, questionData.questionAnswerTime * 1000);

    const questionTable = container.querySelector('.question-table');
    questionTable.innerHTML = '';

    let mainDivs = [];

    let answers = questionData.answers;
    for (let i = 0; i < answers.length; i++) {
        let answer = answers[i];

        let mainDiv = document.createElement('div');
        mainDiv.classList.add('row', 'justify-content-center');

        let button = document.createElement('button');
        button.setAttribute('type', 'button');
        button.classList.add('btn', 'btn-question', 'mb-2', 'ms-2');
        button.dataset.target = i;

        button.addEventListener('click', function (event) {
            const targetElement = event.currentTarget;
            const answerIndex = parseInt(targetElement.dataset.target);

            webSocketClient.send(JSON.stringify(
                {
                    type: 'question_answer',
                    answerIndex: answerIndex,
                    questionId: gameInfo.currentQuestionId
                })
            );
        });

        let answerText = document.createElement('h4');
        answerText.innerText = answer;

        button.appendChild(answerText);
        mainDiv.appendChild(button);

        mainDivs.push(mainDiv);
    }

    shuffle(mainDivs);
    mainDivs.forEach(x => questionTable.appendChild(x));
}