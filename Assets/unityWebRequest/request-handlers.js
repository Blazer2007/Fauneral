/*
* Este ficheiro gere as rotas do servidor de descoberta Node.js.
* Responsável pelo catálogo global de salas (matchmaking).
*/
const mysql = require('mysql2');
const config = require('./config/config.json');
const rooms = {}; // Estrutura em memória: { code: { ip, port, name, isPublic, maxPlayers, currentPlayers, createdAt } }

// Limpeza automática: remove salas inativas há mais de 30 minutos
setInterval(() => {
    const now = Date.now();
    for (const code in rooms) {
        if (now - rooms[code].createdAt > 1000 * 60 * 30) { // 30 minutos
            console.log(`Auto-deleting stale room: ${code}`);
            delete rooms[code];
        }
    }
}, 1000 * 60 * 5); // Verifica a cada 5 min

let data = [{
    id: 1,
    name: "Player1",
    rounds_won: 3
},
{
    id: 2,
    name: "Player2",
    rounds_won: 2
},
{
    id: 3,
    name: "Player3",
    rounds_won: 1
}];

function getData(req, res) {
    res.send(JSON.stringify({ _playerDataInfoArray: data }));
}

function postData(req, res) {
    let receivedData = req.body;
    let name = receivedData.name;
    let rounds_won = receivedData.rounds_won;
    let id = receivedData.id;
    console.log('Received name');
    console.log(name);
    console.log('Received rounds won');
    console.log(rounds_won);
    console.log('Received ID');
    console.log(id);
    res.send('Data received successfully');
}

function getDataFromDatabase(req, res) {
    const connection = mysql.createConnection(config.database);

    connection.connect();

    connection.query('SELECT * FROM players_info', function (error, rows, fields) {
        if (error) {
            console.error('Error fetching data from the database:', error);
            res.status(500).send('Error fetching data');
        }
        else{
            res.send(JSON.stringify({ _playerDataInfoArray: rows }));
        }
    });
    connection.end();
};
function generateCode() {
    return Math.floor(1000 + Math.random() * 9000).toString();
}

function createRoom(req, res) {
    const { ip, port, name, isPublic, maxPlayers } = req.body;
    if (!ip || !port) return res.status(400).send('Missing ip or port');
    
    let code;
    do { code = generateCode(); } while (rooms[code]);
    
    rooms[code] = { 
        ip, 
        port, 
        name: name || `Sala ${code}`,
        isPublic: isPublic === true || isPublic === "true",
        maxPlayers: parseInt(maxPlayers) || 4,
        currentPlayers: 1, // O host conta como 1
        createdAt: Date.now() 
    };
    
    console.log(`Room created: ${code} (${rooms[code].name}) -> ${ip}:${port} [Public: ${rooms[code].isPublic}]`);
    res.json({ code });
}

function getPublicRooms(req, res) {
    const publicList = Object.keys(rooms)
        .map(code => ({ code, ...rooms[code] }))
        .filter(room => room.isPublic === true && room.currentPlayers < room.maxPlayers);
    
    res.json(publicList);
}

function joinRoom(req, res) {
    const { code } = req.params;
    const room = rooms[code];
    if (!room) return res.status(404).send('Room not found');
    
    res.json({ 
        ip: room.ip, 
        port: room.port, 
        name: room.name, 
        maxPlayers: room.maxPlayers, 
        currentPlayers: room.currentPlayers 
    });
}

function updateRoom(req, res) {
    const { code } = req.params;
    const { currentPlayers } = req.body;
    
    if (!rooms[code]) return res.status(404).send('Room not found');
    
    if (currentPlayers !== undefined) {
        rooms[code].currentPlayers = parseInt(currentPlayers);
        // Atualiza o timestamp para manter a sala viva
        rooms[code].createdAt = Date.now();
    }
    
    console.log(`Room updated: ${code}. Players: ${rooms[code].currentPlayers}/${rooms[code].maxPlayers}`);
    res.json({ ok: true });
}

function deleteRoom(req, res) {
    const { code } = req.params;
    if (rooms[code]) {
        console.log(`Room deleted: ${code}`);
        delete rooms[code];
    }
    res.json({ ok: true });
}

module.exports = {
    getData, postData, getDataFromDatabase,
    createRoom, joinRoom, deleteRoom, getPublicRooms, updateRoom
};