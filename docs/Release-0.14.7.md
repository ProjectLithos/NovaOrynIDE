# NovaOryn IDE 0.14.7

Separates verified npm dependency state from completed IDE build state. A failed later compilation no longer makes an already verified dependency tree stale. The installed-dependency verifier also uses immediate ERRORLEVEL handling.
