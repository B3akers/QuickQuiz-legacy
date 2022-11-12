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

async function makeGetRequest(url = '') {
    const response = await fetch(url, {
        method: 'GET',
        mode: 'cors',
        cache: 'no-cache',
        credentials: 'same-origin',
        redirect: 'follow',
        referrerPolicy: 'no-referrer'
    });
    return response.json();
}

function translateCode(code) {
    switch (code) {
        case 'account_created':
            return 'Konto utworzone, sprawdź swój email aby potwierdzić konto!';
        case 'account_exists':
            return 'Konto już istnieje dla podanego adresu email lub użytkownika!';
        case 'invalid_model_register':
            return 'Adres email nie jest poprawny!';
        case 'wrong_email_key':
            return "Kod potwierdzenia nie jest poprwany!";
        case 'email_confirmed':
            return 'Email został potwierdzony!';
        case 'email_sended':
            return 'Email został wysłany ponownie!';
        case 'invalid_credentials':
            return 'Email lub hasło jest nieprawidłowe!';
        case 'email_too_fast':
            return 'Poczekaj przynajmniej 15min przed wysłaniem kolejnego emaila!';
        case 'twitch_connect_error':
            return 'Podczas łączenia konta z Twitch wystąpił błąd!';
        case 'twitch_connect_success':
            return 'Konto zostało pomyślnie połączone z Twitch';
        case 'twitch_invalid_email':
            return 'Twoje konto Twitch musi mieć zweryfikowany email!';
        case 'twitch_email_exists':
            return 'Konto na tym adresie email z Twitch już istnieje!';
        case 'twitch_invalid_scope':
            return 'Błąd Twitch! Brak uprawnień do adresu email!';
        case 'password_reset_success':
            return 'Link do resetu twojego hasła został wysłany na podany adres email!';
        case 'password_reseted':
            return 'Hasło zostało zresetowane, twoje tymczasowe hasło zostało wysłane na twój adres email!';
    }

    return code;
}

function showMessagesFromUrl() {
    const queryString = window.location.search;
    const urlParams = new URLSearchParams(queryString);
    if (urlParams.has('success')) {
        toastr.success(translateCode(urlParams.get('success')));
    }

    if (urlParams.has('error')) {
        toastr.error(translateCode(urlParams.get('error')));
    }
}