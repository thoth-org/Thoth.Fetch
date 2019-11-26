export default function(req, res) {
    res.status(400).jsonp({
        reason: "This always fails."
    });
}
