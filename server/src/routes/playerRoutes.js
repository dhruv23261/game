const express = require('express');
const router = express.Router();
const playerController = require('../controllers/playerController');

router.post('/auth', playerController.authenticatePlayer);
router.get('/:playerId', playerController.getProfile);
router.post('/upgrade', playerController.upgradeStat);

module.exports = router;
