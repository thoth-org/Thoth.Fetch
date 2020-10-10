
function utf8_decode(bytes) {
    let pos = 0;

    const decodeUtf8 = () => {
        const i1 = bytes[pos++];

        if ((i1 & 0x80) === 0) {
            return i1;
        } else if ((i1 & 0xE0) === 0xC0) {
            const i2 = bytes[pos++];
            return (i1 & 0x1F) << 6 | i2 & 0x3F;
        } else if ((i1 & 0xF0) === 0xE0) {
            const i2 = bytes[pos++];
            const i3 = bytes[pos++];
            return (i1 & 0x0F) << 12 | (i2 & 0x3F) << 6 | i3 & 0x3F;
        } else if ((i1 & 0xF8) === 0xF0) {
            const i2 = bytes[pos++];
            const i3 = bytes[pos++];
            const i4 = bytes[pos++];
            return (i1 & 0x07) << 18 | (i2 & 0x3F) << 12 | (i3 & 0x3F) << 6 | i4 & 0x3F;
        } else {
            throw RangeError("Invalid UTF8 byte: " + i1);
        }
    };

    const chars = new Array();

    while (pos < bytes.length) {
        const code = decodeUtf8();
        chars.push(String.fromCodePoint(code));
    }

    return chars.join("");
}

export default (date) => (req, res) => {
    const title = utf8_decode(req.files[0].buffer);
    if (req.path === "/patch/book") {
        res.jsonp({
            "id": 1,
            "title": title,
            "author": "Peter V. Brett",
            "createdAt": date.toISOString(),
            "updatedAt": null
        });
    } else if (req.path === "/patch/author") {
        res.jsonp({
            "id": 1,
            "name": title
        });
    }
};

