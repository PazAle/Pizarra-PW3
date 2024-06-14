var connection = new signalR.HubConnectionBuilder().withUrl("/pizarraHub").build();
var canvas = document.getElementById("pizarra");
var btnGuardar = document.getElementById("guardarBtn");
var btnCargar = document.getElementById("cargarBtn");
var context = canvas.getContext("2d");
var drawing = false;
var prevX = 0, prevY = 0;
var salaActual = "";

// Método para dibujar en la pizarra
function dibujarEnPizarra(data) {
    var dibujo = JSON.parse(data);
    context.beginPath();
    context.moveTo(dibujo.prevX, dibujo.prevY);
    context.lineTo(dibujo.currX, dibujo.currY);
    context.strokeStyle = dibujo.color;
    context.lineWidth = dibujo.size;
    context.stroke();
    context.closePath();
}


function getMousePos(canvas, evt) {
    var rect = canvas.getBoundingClientRect();
    return {
        x: evt.clientX - rect.left,
        y: evt.clientY - rect.top
    };
}

function guardarImagen() {
    const dataURL = canvas.toDataURL();
    localStorage.setItem('savedImage', dataURL);
    alert("Imagen guardada en el almacenamiento local");
}

// Conexión con el hub de SignalR
connection.start().then(function () {
    document.getElementById("crearSala").addEventListener("click", function () {
        salaActual = document.getElementById("salaInput").value;
        connection.invoke("UnirseASala", salaActual).catch(function (err) {
            return console.error(err.toString());
        });
    });

    document.getElementById("sendButton").addEventListener("click", function () {
        var message = document.getElementById("messageInput").value;
        connection.invoke("EnviarMensaje", salaActual, message).catch(function (err) {
            return console.error(err.toString());
        });
        document.getElementById("messageInput").value = "";
    });

    canvas.addEventListener("mousedown", function (e) {
        var pos = getMousePos(canvas, e);
        drawing = true;
        prevX = pos.x;
        prevY = pos.y;
    });

    btnGuardar.addEventListener("click", guardarImagen ());

    btnCargar.addEventListener("click", () => {
        const dataURL = localStorage.getItem('savedImage');
        if (dataURL) {
            const img = new Image();
            img.src = dataURL;
            console.log(img.src);
            img.onload = () => {
                context.clearRect(0, 0, canvas.width, canvas.height);
                context.drawImage(img, 0, 0);
            };
            alert("Imagen cargada desde el almacenamiento local");
        } else {
            alert("No hay imagen guardada en el almacenamiento local");
        }
    });

    canvas.addEventListener("mousemove", function (e) {
        if (drawing && salaActual) {
            var pos = getMousePos(canvas, e);
            var currX = pos.x;
            var currY = pos.y;
            var color = $("#color").val();
            var size = $("#size").val();
            var dibujo = {
                prevX: prevX,
                prevY: prevY,
                currX: currX,
                currY: currY,
                color: color,
                size: size
            };
            dibujarEnPizarra(JSON.stringify(dibujo));
            connection.invoke("Dibujar", salaActual, JSON.stringify(dibujo)).catch(function (err) {
                return console.error(err.toString());
            });
            prevX = currX;
            prevY = currY;
        }
    });

    canvas.addEventListener("mouseup", function () {
        drawing = false;
    });

    canvas.addEventListener("mouseleave", function () {
        drawing = false;
    });

    document.getElementById("limpiar").addEventListener("click", function () {
        context.clearRect(0, 0, canvas.width, canvas.height);
    });

    connection.on("dibujarEnPizarra", function (data) {
        dibujarEnPizarra(data);
    });

    connection.on("RecibirMensaje", function (user, message) {
        var msg = document.createElement("div");
        msg.textContent = user + ": " + message;
        document.getElementById("messagesList").appendChild(msg);
    });

    connection.on("ActualizarUsuarios", function (usuarios) {
        var listaUsuarios = document.getElementById("usuariosConectados");
        listaUsuarios.innerHTML = "";
        usuarios.forEach(function (usuario) {
            var userItem = document.createElement("div");
            userItem.textContent = usuario;
            listaUsuarios.appendChild(userItem);
        });
    });

    connection.on("GuardarImagen", function () {
        guardarImagen();
    });

}).catch(function (err) {
    return console.error(err.toString());
});
