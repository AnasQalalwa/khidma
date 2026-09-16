import { Outlet, useLocation } from 'react-router-dom'
import { Footer } from './Footer'
import { Header } from './Header'

export function Layout() {
  const location = useLocation()
  const isHome = location.pathname === '/'
  const isCatalog = location.pathname === '/catalog'
  const isAuth =
    location.pathname === '/login' || location.pathname === '/register'
  const flush = isHome || isAuth || isCatalog

  return (
    <div className={isAuth ? 'layout layout-auth' : 'layout'}>
      <a className="skip-link" href="#main">
        Skip to content
      </a>
      <Header />
      <main id="main" className={flush ? 'main main-flush' : 'main'}>
        {flush ? <Outlet /> : <div className="container"><Outlet /></div>}
      </main>
      {isAuth ? null : <Footer />}
    </div>
  )
}
