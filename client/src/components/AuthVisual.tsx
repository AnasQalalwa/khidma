import { House, ShieldCheck, Wrench } from 'lucide-react'
import customerLaptop from '../assets/auth-customer-laptop.png'
import serviceProfessional from '../assets/auth-service-professional.png'
import { Icon } from './icons'

export function AuthVisual() {
  return (
    <div className="auth-collage" aria-hidden="true">
      <div className="auth-shape auth-shape-circle" />
      <div className="auth-shape auth-shape-orb" />
      <div className="auth-shape auth-shape-path" />
      <p className="auth-script">
        People
        <br />
        Services
        <br />
        Better Living
      </p>

      <div className="auth-pro-frame">
        <img
          src={serviceProfessional}
          alt=""
          className="auth-pro-photo"
        />
      </div>

      <div className="auth-customer-frame">
        <img
          src={customerLaptop}
          alt=""
          className="auth-customer-photo"
        />
      </div>

      <div className="auth-float-card auth-float-trusted">
        <span className="auth-float-icon">
          <Icon icon={ShieldCheck} size={16} />
        </span>
        <span>
          Trusted
          <br />
          Professionals
        </span>
      </div>

      <div className="auth-float-card auth-float-services">
        <span className="auth-float-icon">
          <Icon icon={Wrench} size={16} />
        </span>
        <span>
          Services
          <br />
          for Every Need
        </span>
      </div>

      <div className="auth-float-card auth-float-home">
        <span className="auth-float-icon">
          <Icon icon={House} size={16} />
        </span>
        <span>
          A cleaner,
          <br />
          brighter tomorrow
        </span>
      </div>

      <div className="auth-plant">
        <span className="auth-leaf auth-leaf-a" />
        <span className="auth-leaf auth-leaf-b" />
        <span className="auth-leaf auth-leaf-c" />
      </div>
    </div>
  )
}
