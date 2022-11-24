async function makePostRequest(url = '', data = {}) {
    const response = await fetch(url, {
        method: 'POST',
        mode: 'cors',
        cache: 'no-cache',
        credentials: 'same-origin',
        headers: {
            'Content-Type': 'application/json',
            'X-Csrf-Token-Value': csrfToken
        },
        redirect: 'follow',
        referrerPolicy: 'no-referrer',
        body: JSON.stringify(data)
    });
    if (response.status > 299 || response.status < 200) {
        return { errorCode: response.status, text: await response.text(), error: 'status_not_success' };
    }
    return response.json();
}

async function makeGetRequest(url = '') {
    const response = await fetch(url, {
        method: 'GET',
        mode: 'cors',
        cache: 'no-cache',
        credentials: 'same-origin',
        redirect: 'follow',
        headers: {
            'X-Csrf-Token-Value': csrfToken
        },
        referrerPolicy: 'no-referrer'
    });
    if (response.status > 299 || response.status < 200) {
        return { errorCode: response.status, text: await response.text(), error: 'status_not_success' };
    }
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
        case 'invalid_password':
            return 'Nieprawidłowe hasło!';
        case 'password_changed':
            return 'Hasło zostało zmienione!';
        case 'username_changed':
            return 'Nazwa użytkownika została zmieniona!'
        case 'connection_removed':
            return 'Połączenie zostało usunięte!';
        case 'invalid_model':
            return 'Nieprawidłowe dane formularza!';
        case 'refresh_success':
            return 'Odswieżenie powiodło się!';
        case 'category_added':
            return 'Kategoria została dodana!';
        case 'record_deleted':
            return 'Rekord został usunięty!';
        case 'category_edited':
            return 'Kategoria została zmieniona!';
        case 'user_not_found':
            return 'Nie znaleziono takiego konta!';
        case 'game_not_found':
            return 'Nie znaleziono takiej gry!';
        case 'question_added':
            return 'Pytanie zostało dodane!';
        case 'question_edited':
            return 'Pytanie zostało zmienione!';
        case 'report_submitted':
            return 'Pytanie zostało zgłoszone!';
        case 'report_handled':
            return 'Zgłoszenie zostało już rozpatrzone!';
        case 'report_decline':
            return 'Zgłoszenie zostało odrzucone!';
        case 'report_accepted':
            return 'Zgłoszenie zostało zaakceptowane!';
        case 'preferences_setted':
            return 'Preferencje zostały zmienione!';
        case 'status_not_success':
            return 'Zapytanie nie powiodło się!';
        case 'image_too_large':
            return 'Obraz który próbujesz przesłać jest zbyt duży, maksymalny rozmiar to 1MB';
        case 'question_request_added':
            return 'Twoje pytanie zostało dodane, zanim ukaże się społeczności zostanie sprawdzone przez moderatorów';
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

function escapeValue(s) {
    if (!s) {
        return '';
    }

    const lookup = {
        '&': "&amp;",
        '"': "&quot;",
        '\'': "&apos;",
        '<': "&lt;",
        '>': "&gt;"
    };
    return s.replace(/[&"'<>]/g, c => lookup[c]);
}

(function () {
    const currentAction = window.location.href.substring(window.location.href.lastIndexOf('/') + 1).toLocaleLowerCase();
    document.querySelectorAll('ul.navbar-nav').forEach(x => {
        x.querySelectorAll('a').forEach(a => {
            const action = a.href.substring(a.href.lastIndexOf('/') + 1).toLocaleLowerCase();
            if (action == currentAction) {
                a.classList.add('active');
            }
        });
    });
})();