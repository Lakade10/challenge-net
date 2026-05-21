<template>
  <div>
    <router-link to="/turnos" style="font-size:14px; color:#1a73e8">← Volver a turnos</router-link>
    <div v-if="turno" class="card" style="margin-top: 20px; max-width: 560px">
      <h2>Turno #{{ turno.id }}</h2>
      <div class="detail-row"><span class="label">Paciente</span><span>{{ turno.paciente?.nombreCompleto }}</span></div>
      <div class="detail-row"><span class="label">DNI</span><span>{{ turno.paciente?.dni }}</span></div>
      <div class="detail-row"><span class="label">Médico</span><span>{{ turno.medico?.nombreCompleto }}</span></div>
      <div class="detail-row"><span class="label">Especialidad</span><span>{{ turno.medico?.especialidad }}</span></div>
      <div class="detail-row"><span class="label">Fecha y hora</span><span>{{ formatFecha(turno.fechaHora) }}</span></div>
      <div class="detail-row"><span class="label">Estado</span><span>{{ turno.estado }}</span></div>
      <div class="detail-row"><span class="label">Motivo</span><span>{{ turno.motivo }}</span></div>

      <div style="margin-top: 24px">
        <div class="form-group">
          <label>Cambiar estado</label>
          <select v-model="nuevoEstado">
            <option v-for="e in estados" :key="e" :value="e">{{ e }}</option>
          </select>
        </div>
        <button @click="cambiarEstado" :disabled="isLoading || this.turno.estado === this.nuevoEstado" style="margin-bottom: 16px">Actualizar estado</button>
      </div>

      <div style="display: flex; gap: 10px">
        <button :disabled="isLoading || this.turno.estado === 'Cancelado'" class="btn-danger" @click="cancelar">Cancelar turno</button>
        <button :disabled="isLoading || this.turno.estado === 'NoShow'" @click="marcarAusencia">Marcar ausencia</button>
      </div>
    </div>
    <p v-else>Cargando...</p>
  </div>
</template>

<script>
import { turnosApi } from '../services/api'

export default {
  name: 'TurnoDetalle',
  data() {
    return {
      turno: null,
      nuevoEstado: 'Pendiente',
      // Cambio: se eliminan Cancelado y NoShow de las opciones de estado, ya que esos estados solo se pueden asignar a través de las acciones específicas)
      estados: ['Pendiente', 'Confirmado', 'Atendido'],
      // Cambio: se agrega isLoading para controlar el estado de las acciones asíncronas y evitar múltiples clicks que puedan generar estados inconsistentes
      isLoading: false
    }
  },
  async mounted() {
    try {
      const res = await turnosApi.getById(this.$route.params.id)
      this.turno = res.data
      this.nuevoEstado = this.turno.estado
    } catch (error) {
      alert(error.response?.data?.mensaje || 'Error al obtener los detalles del turno')
    }
  },
  methods: {
    formatFecha(fecha) {
      // Cambio: formato 24hs en las fechas
      return new Date(fecha).toLocaleString('es-AR', { hour12: false })
    },
    async cambiarEstado() {
      try {
        this.isLoading = true;
        const res = await turnosApi.actualizarEstado(this.turno.id, { estado: this.nuevoEstado })
        // Cambio: como la API solo va a devolver el turno (sin Médico o Paciente incluido) lo único que me interesa actualizar es el estado
        this.turno.estado = res.data.estado
      } catch (error) {
        alert(error.response?.data?.mensaje || 'Error al cambiar el estado del turno')
      } finally {
        this.isLoading = false;
      }
    },
    async cancelar() {
      try {
        this.isLoading = true;
        await turnosApi.cancelar(this.turno.id)
        // Cambio: como la API solo va a devolver el turno (sin Médico o Paciente incluido) lo único que me interesa actualizar es el estado
        this.turno.estado = 'Cancelado'
      } catch (error) {
        console.log("Error completo:", error);
    console.log("¿Tiene response?:", error.response);
        alert(error.response?.data?.mensaje || 'Error al cancelar el turno')
      } finally {
        this.isLoading = false;
      }
    },
    async marcarAusencia() {
      try {
        this.isLoading = true;
        await turnosApi.marcarAusencia(this.turno.id)
        // Cambio: como la API solo va a devolver el turno (sin Médico o Paciente incluido) lo único que me interesa actualizar es el estado
        this.turno.estado = 'NoShow'
      } catch (error) {
        alert(error.response?.data?.mensaje || 'Error al marcar ausencia')
      } finally {
        this.isLoading = false;
      }
    }
  }
}
</script>

<style scoped>
.detail-row {
  display: flex;
  padding: 10px 0;
  border-bottom: 1px solid #f0f0f0;
  font-size: 14px;
}
.label {
  width: 130px;
  font-weight: 600;
  color: #666;
  flex-shrink: 0;
}

button:disabled {
  background-color: #e0e0e0;
  color: #9e9e9e;
  cursor: not-allowed;
  opacity: 0.8;
  border: 1px solid #cccccc;
}
</style>
