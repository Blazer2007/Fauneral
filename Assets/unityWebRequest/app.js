const express=require('express');
const config = require('./config/config.json')
const app=express();
const requesthandlers=require('./request-handlers.js');

app.use(express.json());

app.get('/get-data',requesthandlers.getData);
app.get('/get-data-db', requesthandlers.getDataFromDatabase);
app.post('/post-data', requesthandlers.postData);
app.post('/create-room', requesthandlers.createRoom);
app.get('/join-room/:code', requesthandlers.joinRoom);
app.get('/public-rooms', requesthandlers.getPublicRooms);
app.post('/update-room/:code', requesthandlers.updateRoom);
app.delete('/delete-room/:code', requesthandlers.deleteRoom);


const PORT = process.env.PORT || config.server.port;
app.listen(PORT, () => {
    console.log(`Server is running on port ${PORT}`);
});


