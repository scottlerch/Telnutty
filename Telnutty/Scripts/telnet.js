// Prevent backspace from navigating back in browser
$(document).unbind('keydown').bind('keydown', function (event) {
    var doPrevent = false;
    if (event.keyCode === 8 || event.keyCode === 9 || event.keyCode == 38) {
        var d = event.srcElement || event.target;
        if ((d.tagName.toUpperCase() === 'INPUT' && (d.type.toUpperCase() === 'TEXT' || d.type.toUpperCase() === 'PASSWORD' || d.type.toUpperCase() === 'FILE' || d.type.toUpperCase() === 'EMAIL'))
            || d.tagName.toUpperCase() === 'TEXTAREA') {
            doPrevent = d.readOnly || d.disabled;
        } else {
            doPrevent = true;
        }
    }

    if (doPrevent) {
        event.preventDefault();
    }
});

$(function () {
    // Reference the auto-generated proxy for the hub.
    var telnet = $.connection.telnetHub;

    var rawTelnetData = "";

    telnet.client.telnetDisconnected = function() {
        rawTelnetData = "";
        $('#terminal').html(rawTelnetData);
        $('#connect').val("Connect");
    };

    // Create a function that the hub can call back to display messages.
    telnet.client.telnetAddKeyCodes = function (keyCodes) {

        var keyCodesLength = keyCodes.length;
        for (var i = 0; i < keyCodesLength; i++) {
            if (keyCodes[i] === 13) {
                // Ignore carriage returns otherwise we get double newline in tested use-case, probably because IAC isn't being handled correctly
            }
            else if (keyCodes[i] === 255) {
                // TODO: handle 2-3 byte interpret as command (IAC), ignore for now...
                i++;
                // WILL USE and START USE, ignore 3rd byte
                if (keyCodes[i] === 251 || keyCodes[i] === 253) {
                    i++;
                    // TODO: handle option negotiations
                }
            } else if (keyCodes[i] === 8 && keyCodes[i + 1] === 32) {
                switch (keyCodes[i + 2]) {
                    // BACKSPACE
                    case 8:
                        rawTelnetData = rawTelnetData.substring(0, rawTelnetData.length - 1);
                        break;
                }
                i += 2;
            } else {
                rawTelnetData += String.fromCharCode(keyCodes[i]);
            }
        }

        $('#terminal').html(ansi_up.ansi_to_html(rawTelnetData));

        // Scroll to bottom
        $('#terminal').scrollTop($('#terminal')[0].scrollHeight);
    };
    // Set initial focus to message input box.
    $('#terminal').focus();

    $('#host').val($.url().param('host'));
    $('#port').val($.url().param('port'));

    var host = "";
    var port = "";

    // Start the connection.
    $.connection.hub.start().done(function () {
        $('#terminal').keypress(function (event) {
            // Call the Send method on the hub.
            telnet.server.telnetSendKeyPress(event.keyCode);
        });
        $('#terminal').keydown(function (event) {
            // Call the Send method on the hub.
            telnet.server.telnetSendKeyDown(event.keyCode);
        });
        $('#terminal').keyup(function (event) {
            // Call the Send method on the hub.
            telnet.server.telnetSendKeyUp(event.keyCode);
        });
        $('#connect').click(function () {

            if ($('#connect').val() == "Connect") {

                host = $('#host').val();
                port = $('#port').val();
                telnet.server.telnetConnect(host, port);

                $('#connect').val("Disconnect");
                $('#terminal').focus();

            } else if ($('#connect').val() == "Disconnect") {

                rawTelnetData = "";
                $('#terminal').html(rawTelnetData);

                telnet.server.telnetDisconnect();

                $('#connect').val("Connect");
            }
        });
    });
});