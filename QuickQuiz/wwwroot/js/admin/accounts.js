var accountsDataTable;

(function () {
    const table = document.getElementById('accountsTable');

    accountsDataTable = $(table).DataTable({
        pageLength: 15,
        serverSide: true,
        order: [],
        columns: [
            {
                name: "id",
                data: "id",
                render: function (data, type) {
                    if (type === "display") {
                        return `<a target="_blank" class="text-white" href="${accountProfileAdminUrl}/${data}">${data}</a>`;
                    }
                    return data;
                },
                orderable: false
            },
            {
                name: "username",
                data: "username",
                orderable: false
            },
            {
                name: "email",
                data: "email",
                orderable: false
            },
            {
                name: "isModerator",
                data: "isModerator",
                render: function (data, type) {
                    if (type === "display") {
                        return data ? '✅' : '❌';
                    }
                    return data;
                },
                orderable: false
            },
            {
                name: "isAdmin",
                data: "isAdmin",
                render: function (data, type) {
                    if (type === "display") {
                        return data ? '✅' : '❌';
                    }
                    return data;
                },
                orderable: false
            },
            {
                name: "creationTime",
                data: "creationTime",
                render: function (data, type) {
                    if (type === "display") {
                        if (data > 0)
                            return new Date(data * 1000).toISOString();
                        return '-';
                    }
                    return data;
                },
                orderable: false
            }
        ],
        ajax: {
            url: getAccountsUrl,
            contentType: "application/json",
            type: "POST",
            data: function (d) {
                return JSON.stringify(d);
            }
        }
    });
})();