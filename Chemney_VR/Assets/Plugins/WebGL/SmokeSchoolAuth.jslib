mergeInto(LibraryManager.library, {
  SmokeSchoolGetAuthApi: function () {
    var queryOverride = new URLSearchParams(window.location.search).get("authApi");
    var configured = window.SMOKE_SCHOOL_AUTH && window.SMOKE_SCHOOL_AUTH.apiUrl;
    return stringToNewUTF8(queryOverride || configured || "");
  },

  SmokeSchoolSetLoginOverlayVisible: function (visible) {
    var overlay = document.getElementById("unity-login-overlay");
    if (overlay) overlay.style.display = visible ? "block" : "none";
  },

  SmokeSchoolSetAuthenticationLoading: function (loading) {
    var overlay = document.getElementById("unity-login-overlay");
    var spinner = document.getElementById("unity-login-spinner");
    var submit = document.getElementById("unity-login-submit");
    if (overlay) overlay.setAttribute("aria-busy", loading ? "true" : "false");
    if (spinner) spinner.hidden = !loading;
    if (submit) submit.disabled = !!loading;
  }
});
