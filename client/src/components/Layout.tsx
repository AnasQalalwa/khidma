import { Outlet, useLocation } from 'react-router-dom'
import { Footer } from './Footer'
import { Header } from './Header'

export function Layout() {
  const location = useLocation()
  const flush =
    location.pathname === '/' ||
    location.pathname === '/login' ||
    location.pathname === '/register'

  return (
    <div className="layout">
      <a className="skip-link" href="#main">
        Skip to content
      </a>
      <Header />
      <main id="main" className={flush ? 'main main-flush' : 'main'}>
        {flush ? <Outlet /> : <div className="container"><Outlet /></div>}
      </main>
      <Footer />
    </div>
  )
}
