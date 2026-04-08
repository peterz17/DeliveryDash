mergeInto(LibraryManager.library, {

  FirebaseGoogleSignIn: function() {
    if (typeof firebase === 'undefined' || !firebase.auth) {
      SendMessage('WebGLAuthProvider', 'OnGoogleSignInError', 'Firebase SDK not loaded');
      return;
    }
    var provider = new firebase.auth.GoogleAuthProvider();
    firebase.auth().signInWithPopup(provider).then(function(result) {
      return result.user.getIdToken().then(function(idToken) {
        var data = JSON.stringify({
          idToken: idToken,
          refreshToken: result.user.refreshToken || '',
          displayName: result.user.displayName || '',
          email: result.user.email || '',
          photoURL: result.user.photoURL || ''
        });
        SendMessage('WebGLAuthProvider', 'OnGoogleSignInSuccess', data);
      });
    }).catch(function(error) {
      var msg = (error && error.message) ? error.message : 'Unknown error';
      if (error && error.code === 'auth/popup-closed-by-user') {
        msg = 'Sign-in cancelled';
      }
      SendMessage('WebGLAuthProvider', 'OnGoogleSignInError', msg);
    });
  },

  FirebaseSignOut: function() {
    if (typeof firebase !== 'undefined' && firebase.auth) {
      firebase.auth().signOut();
    }
  },

  FirebaseIsReady: function() {
    return (typeof firebase !== 'undefined' && firebase.auth) ? 1 : 0;
  }

});
