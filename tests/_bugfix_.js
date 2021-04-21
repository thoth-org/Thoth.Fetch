'use strict';
const util = require('util'); // built in node stuff
const Readable = require('stream').Readable;

// I think the problem is that on API creates a WHATWG ReadableStream
// and the other needs a NodeJS ReadableStream
// we wrap one type of stream inside another type, and that fixes it.
// but now it introduces a regression on length, so we monkey patch it too.
// None of this will probably be necessary after node-fetch-3, or not, who knows.

export default function () {
    const gBlob = global.Blob;
    function BlobStream(blobParts = [], options = { type: "" }) {
      Readable.apply(this, blobParts, options);
      const blob = new gBlob(blobParts, options);
      const stream = blob.stream();
      this.wrap(stream);
      this.knownLength = blob.size;
    }
    util.inherits(BlobStream, Readable);
    global.Blob = BlobStream;

    // teach FormData about our CustomBlob thingmaging.
    const gFormData = global.FormData;
    const old_trackLength = gFormData.prototype._trackLength;
    gFormData.prototype._trackLength = function(header, value, options) {
      if (value.knownLength) {
        options = Object.assign(options, { knownLength: value.knownLength });
      }
      return old_trackLength.bind(this)(header, value, options);
    };
};

// ---------------------- DEBUG ----------------------
// const oldCreateServer = require('http').createServer;
// require('http').createServer = (app) => oldCreateServer((request, response, next) => {
//     const requestStart = Date.now();
//     let body = [];
//     let requestErrorMessage = null;

//     const log = (request, response, errorMessage) => {
//         const { rawHeaders, httpVersion, method, socket, url } = request;
//         const { remoteAddress, remoteFamily } = socket;

//         const { statusCode, statusMessage } = response;
//         const headers = response.getHeaders();

//         console.log(
//           JSON.stringify({
//             timestamp: Date.now(),
//             processingTime: Date.now() - requestStart,
//             rawHeaders,
//             body,
//             errorMessage,
//             httpVersion,
//             method,
//             remoteAddress,
//             remoteFamily,
//             url,
//             response: {
//               statusCode,
//               statusMessage,
//               headers
//             }
//           })
//         );
//     };

//     const getChunk = chunk => {
//       body.push(chunk);
//     };
//     const assembleBody = () => {
//       body = Buffer.concat(body).toString();
//     };
//     const getError = error => {
//       requestErrorMessage = error.message;
//     };
//     request.prependListener("data", getChunk);
//     request.prependListener("end", assembleBody);
//     request.prependListener("error", getError);

//     const logClose = () => {
//       removeHandlers();
//       log(request, response, "Client aborted.");
//     };
//     const logError = error => {
//       removeHandlers();
//       log(request, response, error.message);
//     };
//     const logFinish = () => {
//       removeHandlers();
//       log(request, response, requestErrorMessage);
//     };
//     response.on("close", logClose);
//     response.on("error", logError);
//     response.on("finish", logFinish);

//     const removeHandlers = () => {
//       request.off("data", getChunk);
//       request.off("end", assembleBody);
//       request.off("error", getError);
//       response.off("close", logClose);
//       response.off("error", logError);
//       response.off("finish", logFinish);
//     };

//     app(request, response);
// });
// ---------------------- DEBUG ----------------------
