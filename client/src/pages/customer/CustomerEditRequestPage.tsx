import { useParams } from 'react-router-dom'
import { CustomerRequestFormPage } from './CustomerRequestFormPage'

export function CustomerEditRequestPage() {
  const { id } = useParams()
  return <CustomerRequestFormPage requestId={Number(id)} />
}
