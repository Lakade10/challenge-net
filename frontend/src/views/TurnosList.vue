<template>
  <div>
    <h2>Turnos</h2>
    <table v-if="turnos.length">
      <thead>
        <tr>
          <th>#</th>
          <th>Paciente</th>
          <th>Médico</th>
          <th>Especialidad</th>
          <th>Fecha y hora</th>
          <th>Estado</th>
          <th>Motivo</th>
          <th>Acciones</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="turno in turnos" :key="turno.id">
          <td>{{ turno.id }}</td>
          <td>{{ turno.paciente?.nombreCompleto }}</td>
          <td>{{ turno.medico?.nombreCompleto }}</td>
          <td>{{ turno.medico?.especialidad }}</td>
          <td>{{ formatFecha(turno.fechaHora) }}</td>
          <td>
            <span :class="['badge', `badge-${turno.estado?.toLowerCase()}`]">{{ turno.estado }}</span>
          </td>
          <td>{{ truncarMotivo(turno.motivo) }}</td>
          <td>
            <!-- Aca hay que modificar el router-link para que se vea como un botón -->
             <div style="display: flex">
               <button @click="$router.push(`/turnos/${turno.id}`)">Ver</button>
               <button class="btn-danger" style="margin-left: 8px" @click="cancelar(turno.id)" :disabled="loadingTurnoId === turno.id || turno.estado === 'Cancelado'">Cancelar</button>
             </div>
          </td>
        </tr>
      </tbody>
    </table>
    <p v-else>No hay turnos registrados.</p>
  </div>
</template>

<script>
import { turnosApi } from '../services/api'

export default {
  name: 'TurnosList',
  data() {
    return {
      turnos: [],
      // Cambio: se agrega loadingTurnoId para bloquear el botón de cancelar para el turno que se esté cancelando
      loadingTurnoId: null
    }
  },
  async mounted() {
    try {
      const res = await turnosApi.getAll()
      this.turnos = res.data
    } catch {
      alert('Error al procesar la solicitud')
    }
  },
  methods: {
    formatFecha(fecha) {
      // Cambio: formato 24hs en las fechas
      return new Date(fecha).toLocaleString('es-AR', { hour12: false })
    },
    async cancelar(id) {
      try {
        this.loadingTurnoId = id;
        await turnosApi.cancelar(id)
        // Cambio: actualizamos el estado del turno cancelado
        const turno = this.turnos.find(t => t.id === id);
        if (turno) {
          turno.estado = 'Cancelado';
        }
      } catch (error) {
        alert(error.response?.data?.mensaje || 'Error al cancelar el turno')
      } finally {
        this.loadingTurnoId = null;
      }
    },
    // Cambio: añadimos función para truncar a 50 caracteres el motivo y no romper el diseño de la tabla
    truncarMotivo(motivo) {
      if (!motivo) return '';
      return motivo.length > 50 ? motivo.substring(0, 50) + '...' : motivo;
    }
  }
}
</script>

<style scoped>
.badge {
  padding: 3px 10px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 600;
}
.badge-pendiente   { background: #fff3cd; color: #856404; }
.badge-confirmado  { background: #d4edda; color: #155724; }
.badge-cancelado   { background: #f8d7da; color: #721c24; }
.badge-atendido    { background: #d1ecf1; color: #0c5460; }
.badge-noshow      { background: #e2e3e5; color: #383d41; }
button:disabled {
  background-color: #e0e0e0;
  color: #9e9e9e;
  cursor: not-allowed;
  opacity: 0.8;
  border: 1px solid #cccccc;
}
</style>
