mergeInto(LibraryManager.library, {
    InternalMiddlewareReady: function () {
        let ready = false;
        if (window.parent && window.parent.window.WebGLMessage) {
            ready = true;
        } else {
            console.warn("[JSLIB] WebGLMessage is not available.");
        }
        return ready;
    },
    WebGLReady: function () {
        console.log(`[JSLIB] Calling WebGLReady`);
        //if (!LibraryManager.library.InternalMiddlewareReady()) return;

        window.parent.window.WebGLMessage("WebGLReady", "");
    },
    OpenConnectionPage: function () {
        console.log(`[JSLIB] Calling OpenConnectionPage`);
        //if (!LibraryManager.library.InternalMiddlewareReady()) return;

        window.parent.window.WebGLMessage("OpenConnectionPage", "");
    },
    ClearSession: function () {
        console.log(`[JSLIB] Calling ClearSession`);
        //if (!LibraryManager.library.InternalMiddlewareReady()) return;

        window.parent.window.WebGLMessage("ClearSession", "");
    },
    ExecuteCreatePlayer: function () {
        console.log(`[JSLIB] Calling ExecuteCreatePlayer`);
        //if (!LibraryManager.library.InternalMiddlewareReady()) return;

        window.parent.window.WebGLMessage("ExecuteCreatePlayer", "");
    },
    ExecuteCreateGame: function () {
        console.log(`[JSLIB] Calling ExecuteCreateGame`);
        //if (!LibraryManager.library.InternalMiddlewareReady()) return;

        window.parent.window.WebGLMessage("ExecuteCreateGame", "");
    },
});
