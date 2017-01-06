const gm = require('gm');

module.exports = function (imagePaths) {
    let magic = gm().in('-page', '+0+0');

    imagePaths.forEach((path, index) => {
        magic.in('-page', `+${index%2*320}+${index/2*304}`)
            .in(path);
    });

    magic.mosaic()
        .write('tesOutput.jpg', function (err) {
            if (err) console.log(err);
        });
};