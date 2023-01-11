var dataTable;

$(document).ready(function () {
    var url = window.location.search;
    if (url.includes("inprocess")) {
        loadDataTable("inprocess");
    }
    else {
        if (url.includes("refunded")) {
            loadDataTable("refunded");
        }
        else {
            loadDataTable("all");
        }
    }
});

function loadDataTable(status) {
    dataTable = $('#refundData').DataTable({
        "ajax": {
            "url": "/Admin/Refund/GetAll?status=" + status
        },
        "columns": [
            { "data": "id", "width": "20%" },
            { "data": "orderStatus", "width": "20%" },
            { "data": "city", "width": "20%" },
            {
                "data": "id",
                "render": function (data) {
                    return `
                        <div class="w-75 btn-group" role="group">
                        <a href="/Admin/Refund/Details?orderHeaderId=${data}" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i>Details</a>
                        </div>
                    `
                },
                "width": "20%"
            },
        ]
    });
}

//