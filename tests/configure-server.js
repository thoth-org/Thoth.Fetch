function unitViaNoBody (req, res) {
    res.send();
}

function unitViaNoBodyAndNonDefaultStatusCode (req, res) {
    res.status(204).send();
}

export default function(server) {
    server.delete("/fake-delete", function (req, res) {
        res.jsonp({
            isSuccess : true
        });
    });

    server.get("/get/unit-via-no-body-and-non-default-status-code", unitViaNoBodyAndNonDefaultStatusCode);
    server.post("/post/unit-via-no-body-and-non-default-status-code", unitViaNoBodyAndNonDefaultStatusCode);
    server.delete("/delete/unit-via-no-body-and-non-default-status-code", unitViaNoBodyAndNonDefaultStatusCode);
    server.put("/put/unit-via-no-body-and-non-default-status-code", unitViaNoBodyAndNonDefaultStatusCode);
    server.patch("/patch/unit-via-no-body-and-non-default-status-code", unitViaNoBodyAndNonDefaultStatusCode);

    server.get("/get/unit-via-no-body", function (req, res) {
        res.send();
    });
    server.post("/post/unit-via-no-body", function (req, res) {
        res.send();
    });
    server.delete("/delete/unit-via-no-body", function (req, res) {
        res.send();
    });
    server.put("/put/unit-via-no-body", function (req, res) {
        res.send();
    });
    server.patch("/patch/unit-via-no-body", function (req, res) {
        res.send();
    });
}
