const crx3 = require('crx3');

crx3(['addon/manifest.json'], {
    keyPath: 'addon.pem',
    crxPath: 'ennlpladclaaogmhlddghpneajafmgln.crx',
    zipPath: 'ennlpladclaaogmhlddghpneajafmgln.zip',
    xmlPath: 'ennlpladclaaogmhlddghpneajafmgln.xml',
    crxURL: 'https://github.com/open-rpa/openrpa/blob/master/OpenRPA.NativeMessagingHost/addon.crx'
})
    .then(() => console.log('done'))
    .catch(console.error)
    ;