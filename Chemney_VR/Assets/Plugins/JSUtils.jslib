var plugin = {
    OpenTab : function(url)
    {
        url = Pointer_stringify(url);
        window.open(url,'_blank');
    },
    SmokeSchoolExitWebXR : function()
    {
        if (typeof Module === 'undefined' || !Module.WebXR) {
            return;
        }

        var xrSession = Module.WebXR.xrSession;
        if (xrSession && xrSession.isInSession && Module.WebXR.toggleVR) {
            Module.WebXR.toggleVR();
        }
    },
};
mergeInto(LibraryManager.library, plugin);
