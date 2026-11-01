# Miten käyttäjä valitsee ja vaihtaa tenantin

Nov 11, 2026 · @Marko

Käyttäjä voi kuulua useaan tenanttiin (yritykseen), ja jokainen kirjautuminen on sidottu yhteen niistä. Tuotesovellus näyttää käyttäjän yritykset valikossa, ja yritystä vaihdettaessa auth server antaa uuden tokenin valitulle yritykselle.

Osat ovat kolme:

- **Auth server** tietää, mihin tenantteihin käyttäjä kuuluu, ja antaa tokenin.
- **Tuotesovellus** (React) näyttää valikon ja pyytää uuden tokenin vaihdon yhteydessä.
- **Selain** pitää kirjautuneen käyttäjän ja tokenin muistissa.

## 1. Kirjautuminen

Kun käyttäjä kirjautuu, tuotesovellus ohjaa hänet auth serverille (`signinRedirect`). Auth server antaa takaisin access tokenin ja ID tokenin. Access token sisältää muun muassa:

- `sub`: käyttäjän id
- `client_id`: tuotesovelluksen client
- `tenant_id`: se yritys, jonka käyttäjä on tällä hetkellä valinnut

Sovellus lukee `tenant_id`:n tokenista (`decodeJwt`) ja tietää siitä, mikä yritys valikossa on valittuna.

## 2. Tenanttilista

Valikon lista tulee auth serverin `/connect/userinfo`-päätepisteestä. Sovellus saa sen osana käyttäjän profiilia (`user.profile.tenants`). Päätepiste tekee näin:

1. Lukee tokenista käyttäjän id:n (`sub`) ja clientin (`client_id`).
2. Hakee clientin resource serverin eli tuotesovelluksen.
3. Hakee ne tenantit, joissa käyttäjällä on rooli tässä tuotesovelluksessa (`GetUserTenantsAsync`). Vain aktiiviset tenantit tulevat mukaan.
4. Palauttaa ne listana, jossa on kunkin yrityksen `id` ja `name`.

## 3. Yrityksen vaihto

Kun käyttäjä valitsee valikosta toisen yrityksen, `TenantSelect` kutsuu:

```ts
auth.signinRedirect({ extraQueryParams: { tenant: tenantId } })
```

Selain menee auth serverille, joka tunnistaa käyttäjän ja antaa uuden tokenin, jonka `tenant_id` on valittu yritys. Sovellus palaa takaisin uudella tokenilla, ja valikossa näkyy uusi yritys.

Vaihto tehdään vain, jos `tenantId` on eri kuin nykyinen, joten turhia siirtymiä ei tule.

## Mistä lista tulee

Lista ei ole erillinen tieto, vaan se johdetaan käyttäjän rooleista. Jokaisella roolirivillä on käyttäjä, rooli ja tenantti. Kun käyttäjä kutsutaan yritykseen (`/api/user/invite`), hänelle lisätään rooli kyseiseen tenanttiin, ja yritys ilmestyy hänen listalleen.

Siksi käyttäjä, joka on kutsuttu kahteen yritykseen, näkee valikossa kaksi yritystä. Käyttäjä, jolta rooli poistetaan yrityksestä, ei näe sitä enää.

## Yhteenveto

1. Käyttäjä kirjautuu, ja token kertoo nykyisen yrityksen (`tenant_id`).
2. Sovellus hakee yritysten listan `/connect/userinfo`-päätepisteestä.
3. Valikon valinta kutsuu `signinRedirect` uudelleen parametrilla `tenant`, ja auth server antaa uuden tokenin valitulle yritykselle.
4. Lista syntyy käyttäjän rooleista: kutsu lisää yrityksen listalle, roolin poisto poistaa sen.
