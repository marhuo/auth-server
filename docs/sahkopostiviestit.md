# Sähköpostiviestien ulkoasu

*Päätetty ja toteutettu 25.11.2026.*

Auth server lähettää kaksi viestiä: kirjautumiskoodin ja kutsun. Viestit ovat suomeksi ja englanniksi.

## 1. Pohjat

Jokainen viesti on oma HTML-tiedosto kansiossa `backend/AuthServer.Infrastructure/Users/EmailTemplates/`:

- `sign-in-code.fi.html`, `sign-in-code.en.html`
- `invite.fi.html`, `invite.en.html`

Tiedostoa muokataan kuin tavallista HTML-sivua, ja sen voi avata selaimessa. Muutos tulee käyttöön, kun projekti käännetään: tiedostot pakataan sovellukseen ja luetaan ensimmäisellä käyttökerralla.

- **Paikkamerkit:** `{{AppName}}`, `{{Code}}`, `{{Link}}`, `{{Minutes}}`, `{{TenantName}}`, `{{Email}}`. Ne on lueteltu jokaisen tiedoston alussa.
- **Aihe:** `<title>`-elementin teksti.
- **Ulkoasu:** sama kaikille sovelluksille. Ylhäällä sovelluksen nimi, nappi on musta kuten admin-UI:n primary-nappi.
- **Sovelluksen nimi:** clientin `DisplayName`. Ilman clientia (kirjautuminen suoraan auth serveriin, kutsu admin-UI:sta) auth serverin oma nimi asetuksesta `Email:AppName`.
- Sähköpostin HTML on yksinkertaista: taulukot asettelussa ja CSS `style`-attribuuteissa, koska Outlook ja Gmail eivät tue muuta.

## 2. Hylätty: HTML-editori (esim. TinyMCE) ja clientkohtainen ulkoasu

- Editorilla tehty viesti rikkoutuu helposti jossain sähköpostiohjelmassa.
- Pohja voi rikkoutua vahingossa: jos `{{Code}}` poistuu, kukaan ei saa koodia.
- Vapaa HTML auth serverin nimissä lähetetyssä viestissä mahdollistaa huijausviestit.
- Clientkohtainen logo ja väri (25.11.2026): jätettiin pois, sovelluksen nimi riittää.

## 3. Lähetys

Viestejä ei vielä lähetetä oikeasti: otsikko ja HTML kirjoitetaan lokiin (`LoggingUserEmailSender`), josta HTML:n voi kopioida ja avata selaimessa. Oikea lähetyspalvelu (SMTP, Postmark, Amazon SES) lisätään myöhemmin.

## 4. Kieli

- **Kirjautumiskoodi:** kieli selaimen kielestä. Selain lähettää jokaisessa pyynnössä otsakkeen `Accept-Language` (esim. `fi-FI,fi;q=0.9,en;q=0.8`), joka tulee käyttöjärjestelmän tai selaimen kieliasetuksesta. Kirjautumissivu valitsee siitä ensimmäisen suomen tai englannin; muuten suomi.
- **Kutsu:** kutsupyynnössä kenttä `Language` (`fi` / `en`), oletus `fi`. Admin-UI:n kutsut ovat suomeksi.

## 5. Kutsun sisältö

Otsikko ja aihe "Yritys A kutsui sinut sovellukseen TuntiQ". Teksti kertoo, millä osoitteella kirjaudutaan ja ettei salasanaa tarvita (huijausviestit kysyvät salasanaa). Nappi "Avaa TuntiQ". Lopussa osoite, johon kutsu lähetettiin.

- **Tenantti:** kutsun roolin tenantti. Tekstissä ei sanota "yritys", koska tenantti voi olla myös tiimi tai muu ryhmä. Admin-UI:n kutsussa ei ole tenanttia, joten sen tilalla on auth serverin nimi (`Email:AppName`).
- Kutsujaa ei näytetä: auth serverissä ei ole käyttäjien nimiä. Pohjissa ei ole ehtoja.

## 6. Avoimet kysymykset

- Mikä lähetyspalvelu, kun viestit lähetetään oikeasti?
